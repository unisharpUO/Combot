using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using ScriptSDK.API;
using ScriptSDK.Mobiles;

namespace Combot.Server
{
    using Data;
    using Routines;

    /// <summary>
    /// Main combot class
    /// </summary>
    public class Combot
    {
        #region Properties
        /// <summary>
        /// PlayerMobile type of the profile the server is running on
        /// </summary>
        public static PlayerMobile Self = PlayerMobile.GetPlayer();
        /// <summary>
        /// ID of the profile the server is running on
        /// </summary>
        public static uint combotID = Self.Serial.Value;
        /// <summary>
        /// Name of the profile the server is running on
        /// </summary>
        public static string combotName = Self.Name;
        #endregion

        #region Methods
        /// <summary>
        /// Method for posting messages to the console
        /// with a timestamp
        /// </summary>
        /// <param name="message">string: message to send to the console</param>
        /// <param name="args">(optional)object[]: additional arguments</param>
        public static void ConsoleMessage(string message, params object[] args)
        {
            Console.Write("[{0}-{1}] ", Combot.combotName, DateTime.Now.ToString("mm:ss"));
            Console.WriteLine(message, args);
        }

        /// <summary>
        /// Method for posting messages to the console
        /// with a timestamp and optional color
        /// </summary>
        /// <param name="message">string: message to send to the console</param>
        /// <param name="color">(optional)enum ConsoleColor: the color you want the message to be</param>
        /// <param name="args">(optional)object[]: additional arguments</param>
        public static void ConsoleMessage(string message, ConsoleColor color = ConsoleColor.White, params object[] args)
        {
            Console.Write("[{0}-{1}] ", Combot.combotName, DateTime.Now.ToString("mm:ss"));
            Console.ForegroundColor = color;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }
        #endregion

        #region Start
        /// <summary>
        /// Main startup class
        /// </summary>
        /// <param name="args">args</param>
        [STAThread]
        public static void Main(string[] args)
        {
            A:
            if (Stealth.Client.GetConnectedStatus())
            {
                Combot.ConsoleMessage("Connected to Stealth UO profile",
                    ConsoleColor.DarkGray);
                Routine.init(); // Run each routine in a new thread
                Server.Start(); // Starts server
            }
            else
            {
                Combot.ConsoleMessage("Selected profile is not online, retrying in 5 seconds",
                    ConsoleColor.DarkGray);
                Thread.Sleep(5000);
                goto A;
            }
        }
        #endregion

        #region Routines
        /// <summary>
        /// Class containing all the routines for the connected
        /// stealth profile to run, each routine is ran in a
        /// separate thread
        /// </summary>
        public class Routine
        {
            /// <summary>
            /// Initialize routines
            /// </summary>
            public static void init()
            {
                try
                {
                    /* Each routine gets started in a
                     * new thread.
                     * */
                    Thread CombatRoutineThread = new Thread(() => Combat.routine());
                    CombatRoutineThread.Start();
                    // Main thread runs back to start server
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                    System.Threading.Thread.Sleep(15000);
                    //this message will self destruct in 15 seconds
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Contains methods to start listening for clients
    /// and holds the job queue
    /// </summary>
    public class Server
    {
        #region Properties
        static Socket listenerSocket;

        /// <summary>
        /// A list of connected clients
        /// </summary>
        public static List<ClientData> _clients;

        /// <summary>
        /// A list of jobs queued by the server
        /// listed in order of priority set by
        /// the client
        /// </summary>
        public static PriorityQueue<Job> jobQueue = new PriorityQueue<Job>();
        #endregion

        #region Socket/Methods
        /// <summary>
        /// Method that starts listening for clients
        /// </summary>
        public static void Start()
        {
            Combot.ConsoleMessage("Starting server",
                ConsoleColor.DarkGray);

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _clients = new List<ClientData>();
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.getIp4Address()), 1069);
            listenerSocket.Bind(ip);

            //Start listening for connections in a new thread so the application can continue
            Thread ListenThread = new Thread(listenThread);
            ListenThread.Start();

            Combot.ConsoleMessage("Success, listening IP:{0}:1069",
                ConsoleColor.DarkGray, Packet.getIp4Address());

            /* *
             * This an event timer that sends a
             * PriorityQueue Notify method every
             * ten seconds.
             * * */
            System.Timers.Timer notifyTimer = new System.Timers.Timer(1000);
            notifyTimer.Elapsed += notifyEvent;
            notifyTimer.Enabled = true;
        }

        static void notifyEvent(object sender, EventArgs e)
        {
            jobQueue.notify();
        }

        /// <summary>
        /// Infinite loop listening for new clients
        /// </summary>
        static void listenThread()
        {
            for (; ; )
            {
                listenerSocket.Listen(0);
                _clients.Add(new ClientData(listenerSocket.Accept()));
            }
        }

        /// <summary>
        /// Data being received from a client
        /// </summary>
        /// <param name="cSocket">Socket of the client to receive data from</param>
        public static void incomingData(object cSocket)
        {
            Socket clientSocket = (Socket)cSocket;
            byte[] Buffer;
            int readBytes;

            for (; ; )
            {
                try
                {
                    Buffer = new byte[clientSocket.SendBufferSize];
                    readBytes = clientSocket.Receive(Buffer);

                    if (readBytes > 0)
                    {
                        Packet p = new Packet(Buffer);
                        dataManager(p);
                    }
                }
                catch (SocketException sx)
                {
                    Combot.ConsoleMessage("{0} has disconnected: {1}",
                        ConsoleColor.DarkGray,
                        removeClientInfo(clientSocket).ToString(),
                        sx.Message.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// Removes the client from the list of
        /// connected clients
        /// </summary>
        /// <param name="socket">Socket which the
        /// client is connected to</param>
        private static string removeClientInfo(Socket socket)
        {
            for (int i = 0; i < _clients.Count; i++)
                if (_clients[i].clientSocket == socket)
                {
                    string result = _clients[i].clientName;
                    _clients.RemoveAt(i);
                    return result;
                }
            return "Couldn't find client";
        }

        /// <summary>
        /// Manages a packet sent from
        /// a client and does something
        /// based on what type of packet
        /// was sent
        /// </summary>
        /// <param name="p">a packet,
        /// caught by incomingData</param>
        public static void dataManager(Packet p)
        {
            try
            {
                switch (p.packetType)
                {
                    case PacketType.chat:
                        foreach (ClientData c in _clients)
                            c.clientSocket.Send(p.toBytes());
                        break;
                    case PacketType.job:
                        switch (p.Gdata[1])
                        {
                            case "accepted": //set job to assigned
                                Job acceptJob = jobQueue.Find(p.packetJobID);
                                acceptJob.Assigned = true;
                                Combot.ConsoleMessage("{0} accepted the job",
                                    ConsoleColor.DarkGreen, p.Gdata[0]);
                                break;

                            case "complete": //remove the job
                                jobQueue.Remove(p.packetJobID);
                                Combot.ConsoleMessage("{0} completed a job",
                                    ConsoleColor.Green, p.Gdata[0]);
                                break;

                            case "rejected":
                                Job rejectJob = jobQueue.Find(p.packetJobID);
                                rejectJob.Assigned = false;
                                ClientData rejectClient = _clients.Where(x => x.id == p.senderId).First();
                                var id = rejectClient.clientId;
                                rejectJob.Rejectors.Add(id);
                                if (rejectJob.Rejectors.Count == _clients.Count)
                                    rejectJob.Rejectors.Clear();
                                Combot.ConsoleMessage("{0} rejected the job, reason: {1}",
                                    ConsoleColor.DarkYellow, p.Gdata[0], p.Gdata[2]);
                                break;

                            case "add": //client wants to add a job
                                jobQueue.enqueue(p.packetJob);
                                break;

                            default: //when all else fails
                                Combot.ConsoleMessage("{0} sent a job packet I could not interpret",
                                    ConsoleColor.Yellow, p.Gdata[0]);
                                break;
                        }
                        break;

                    case PacketType.registration:
                        foreach (ClientData c in _clients)
                            if (c.id == p.Gdata[0].ToString())
                            {
                                c.clientName = p.Gdata[1].ToString();
                                c.clientId = Convert.ToUInt32(p.Gdata[2]);
                                Combot.ConsoleMessage("{0} has connected to the server",
                                    ConsoleColor.Gray, c.clientName);
                            }
                        break;

                    case PacketType.status:
                        switch (p.Gdata[1])
                        {
                            case "IsCasting":
                                ClientData _clientCasting = _clients.Where(x => x.id == p.senderId).First();
                                _clientCasting.IsCasting = p.status;
                                /*Combot.ConsoleMessage("{0} {1} {2}",
                                    ConsoleColor.DarkCyan, _clientCasting.clientName, p.Gdata[1], p.status);*/
                                break;
                            case "IsBusy":
                                ClientData _clientBusy = _clients.Where(x => x.id == p.senderId).First();
                                _clientBusy.IsBusy = p.status;
                                /*Combot.ConsoleMessage("{0} {1} {2}",
                                    ConsoleColor.DarkCyan, _clientBusy.clientName, p.Gdata[1], p.status);*/
                                break;
                            default:

                                break;

                        }
                        break;

                    default:
                        Combot.ConsoleMessage("{0} sent a packet I could not interpret",
                            ConsoleColor.Yellow, p.Gdata[0]);
                        break;
                }
            }
            catch (Exception x)
            {
                Combot.ConsoleMessage(x.Message.ToString(),
                    ConsoleColor.Red);
                Combot.ConsoleMessage(x.GetType().FullName,
                    ConsoleColor.Red);
            }
        }

        /// <summary>
        /// The client object contains
        /// methods for handling client
        /// data
        /// </summary>
        public class ClientData
        {
            /// <summary>
            /// Socket that the client is connected to
            /// </summary>
            public Socket clientSocket;

            /// <summary>
            /// Thread that the connection is running on
            /// </summary>
            public Thread clientThread;

            /// <summary>
            /// Name of the client's connected profile
            /// </summary>
            public string clientName;

            /// <summary>
            /// ID of the client's connected profile
            /// </summary>
            public uint clientId;

            /// <summary>
            /// True if the client's profile is casting
            /// </summary>
            public bool IsCasting;

            /// <summary>
            /// True if the client's profile is busy
            /// </summary>
            public bool IsBusy;

            /// <summary>
            /// Guid given by server
            /// </summary>
            public string id;

            /// <summary>
            /// 
            /// </summary>
            public ClientData()
            {
                id = Guid.NewGuid().ToString();
                clientThread = new Thread(Server.incomingData);
                clientThread.Start(clientSocket);
                sendregistrationPacket();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="clientSocket"></param>
            public ClientData(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                id = Guid.NewGuid().ToString();
                clientThread = new Thread(Server.incomingData);
                clientThread.Start(clientSocket);
                sendregistrationPacket();
            }

            /// <summary>
            /// Client sends a packet with a Guid
            /// so the server can identify us
            /// </summary>
            public void sendregistrationPacket()
            {
                Packet p = new Packet(PacketType.registration, "server");
                p.Gdata.Add(id);
                clientSocket.Send(p.toBytes());
            }
        }
        #endregion

    }

    /// <summary>
    /// Queue list of items that are
    /// sorted by priority
    /// </summary>
    /// <typeparam name="T">generic type</typeparam>
    public class PriorityQueue<T> : Server
        where T : Job
    {
        #region Properties
        private List<T> data;
        /// <summary>
        /// PriorityQueue represents a list of items
        /// </summary>
        public PriorityQueue()
        {
            this.data = new List<T>();
        }
        private readonly Object locker = new Object();
        private readonly Object notifyLocker = new Object();
        #endregion

        #region Methods
        /// <summary>
        /// Add items to the queue,
        /// will only add if it doesn't
        /// already exist
        /// </summary>
        /// <param name="item"></param>
        public void enqueue(T item)
        {
            lock (locker)
            {
                try
                {
                    //check if the job already exists in the list by comparing the job type and targetid
                    if (!this.data.Any(x => x.Type == item.Type && x.TargetID == item.TargetID))
                    {
                        data.Add(item);
                        int ci = data.Count - 1; // child index; start at end
                        while (ci > 0)
                        {
                            int pi = (ci - 1) / 2; // parent index
                            if (data[ci].CompareTo(data[pi]) >= 0) break; // child item is larger than (or equal) parent so we're done
                            T tmp = data[ci]; data[ci] = data[pi]; data[pi] = tmp;
                            ci = pi;
                        }
                        //notify();
                    }
                }
                catch (Exception x)
                {
                    Combot.ConsoleMessage(x.Message.ToString(),
                        ConsoleColor.Red);
                    Combot.ConsoleMessage(x.GetType().FullName,
                        ConsoleColor.Red);
                }
            }
        }

        private void PurgeOld()
        {
            List<Job> RemoveList = new List<Job>();
            foreach (T item in data)
            {
                TimeSpan _duration = DateTime.Now - item.Created;
                if (_duration.Seconds > 10)
                    RemoveList.Add(item);
            }
            foreach (T item in RemoveList)
            {
                jobQueue.Remove(item.ID);
            }
        }

        /// <summary>
        /// Notifies clients of pending jobs in the queue
        /// </summary>
        public void notify()
        {
            lock (notifyLocker)
            {
                try
                {
                    PurgeOld();

                    if (_clients.Count > 0 && data.Count > 0)
                    {
                        foreach (T item in data.Where(x => x.Assigned == false))
                        {
                            item.Assigned = true;

                            ClientData c;

                            //make a list of people whose id from rejectors does not belong in the list
                            List<ClientData> cTemp = _clients.Where(x => !item.Rejectors.Contains(x.clientId)).ToList();

                            if (item.Args == "spell")
                            {
                                c = cTemp.Where(x => !x.IsCasting).First();
                                c.IsCasting = true;
                            }
                            else
                            {
                                c = cTemp.Where(x => !x.IsBusy).First();
                                c.IsBusy = true;
                            }

                            Packet p = new Packet(PacketType.job, "server", item);
                            p.Gdata.Add("server");
                            p.Gdata.Add(item.ToString());

                            /*Combot.ConsoleMessage("Status before sending packet: {0}, {1}",
                                ConsoleColor.DarkMagenta, c.IsBusy, c.IsCasting);*/

                            c.clientSocket.Send(p.toBytes());

                            /*Combot.ConsoleMessage("Status after sending packet: {0}, {1}",
                                ConsoleColor.DarkMagenta, c.IsBusy, c.IsCasting);*/
                        }
                        /*Combot.ConsoleMessage("Pending jobs: {0}",
                            ConsoleColor.DarkGray, jobQueue.ToPost());*/
                    }
                }
                catch (Exception x)
                {
                    Combot.ConsoleMessage(x.Message.ToString(),
                        ConsoleColor.Red);
                    Combot.ConsoleMessage(x.GetType().FullName,
                        ConsoleColor.Red);
                }
            }
        }

        /// <summary>
        /// Searches for a job in the list
        /// </summary>
        /// <param name="jobID">Guid of the job to find</param>
        /// <returns></returns>
        public T Find(Guid jobID)
        {
            foreach (T j in data)
                if (j.ID == jobID)
                    return j;
            return null;
        }

        /// <summary>
        /// Returns true if the job was removed
        /// </summary>
        /// <param name="jobID">Guid of the job to remove</param>
        /// <returns></returns>
        public void Remove(Guid jobID)
        {
            lock (locker)
            {
                try
                {
                    int li = data.Count - 1; // last index (before removal)
                    foreach (T j in data)
                        if (j.ID == jobID)
                        {
                            data.Remove(j);
                            break;
                        }
                    --li; // last index (after removal)
                    int pi = 0; // parent index. start at front of pq
                    while (true)
                    {
                        int ci = pi * 2 + 1; // left child index of parent
                        if (ci > li) break;  // no children so done
                        int rc = ci + 1;     // right child
                        if (rc <= li && data[rc].CompareTo(data[ci]) < 0) // if there is a rc (ci + 1), and it is smaller than left child, use the rc instead
                            ci = rc;
                        if (data[pi].CompareTo(data[ci]) <= 0) break; // parent is smaller than (or equal to) smallest child so done
                        T tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp; // swap parent and child
                        pi = ci;
                    }
                }
                catch (Exception x)
                {
                    Combot.ConsoleMessage(x.Message.ToString(),
                    ConsoleColor.Red);
                    Combot.ConsoleMessage(x.GetType().FullName,
                        ConsoleColor.Red);
                }
            }
        }
        
        /// <summary>
        /// Return the item with the highest priority
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            T frontItem = data[0];
            return frontItem;
        }

        /// <summary>
        /// Counts the items in the list
        /// </summary>
        /// <returns>int</returns>
        public int Count()
        {
            return data.Count;
        }

        /// <summary>
        /// Returns the entire list and all it's items as a string
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < data.Count; ++i)
                s += data[i].ToString() + " ";
            s += "count = " + data.Count;
            return s;
        }

        /// <summary>
        /// Returns the entire list of jobs with just the type and target
        /// </summary>
        /// <returns></returns>
        public string ToPost()
        {
            string s = "";
            s += "(Total: " + data.Count + ") ";
            for (int i = 0; i < data.Count; ++i)
                s += "[" + data[i].Type + "->" + data[i].TargetID + "?" + data[i].Assigned + "] ";
            s += "count = " + data.Count;
            return s;
        }

        /// <summary>
        /// Used for comparing job priorities
        /// </summary>
        /// <returns>bool</returns>
        public bool IsConsistent()
        {
            // is the heap property true for all data?
            if (data.Count == 0) return true;
            int li = data.Count - 1; // last index
            for (int pi = 0; pi < data.Count; ++pi) // each parent index
            {
                int lci = 2 * pi + 1; // left child index
                int rci = 2 * pi + 2; // right child index

                if (lci <= li && data[pi].CompareTo(data[lci]) > 0) return false; // if lc exists and it's greater than parent then bad.
                if (rci <= li && data[pi].CompareTo(data[rci]) > 0) return false; // check the right child too.
            }
            return true; // passed all checks
        }
        #endregion
    }
}
