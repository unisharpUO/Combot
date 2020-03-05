using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Combot
{
    using Data;

    public class Client
    {
        public static Socket master;
        public static string id;

        public static void Connect()
        {
            string _ip = Packet.getIp4Address();
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(_ip), 1069);

            bool _connected = false;
            do
            {
                try
                {
                    master.Connect(ipe);
                    _connected = true;
                }
                catch
                {
                    Controller.ConsoleMessage("Could not connect to server, retrying in 5 seconds",
                       ConsoleColor.DarkGray);
                    Thread.Sleep(5000);
                }
            } while (!_connected);

            Thread t = new Thread(incomingData);
            t.Start();
        }

        /// <summary>
        /// Method to receive data from teh server,
        /// this method is started in its own thread
        /// </summary>
        static void incomingData()
        {
            byte[] buffer;
            int readBytes;

            for (; ; )
            {
                try
                {
                    buffer = new byte[master.SendBufferSize];
                    readBytes = master.Receive(buffer);
                    if (readBytes > 0)
                        DataManager(new Packet(buffer));
                }
                catch (SocketException sx)
                {
                    Controller.ConsoleMessage("Disconnected from server: {0}",
                        ConsoleColor.DarkGray,
                        sx.Message.ToString());
                    Thread.Sleep(5000);
                    break;
                }
            }

            /*
             * The thread will only make it here
             * if there is a socket exception thrown
             * above, in which case we want to
             * reconnect to the server
             * * */
            try
            {
                Connect();
            }
            catch (Exception e)
            {
                Controller.ConsoleMessage(e.Message.ToString(),
                    ConsoleColor.Red);
                Controller.ConsoleMessage(e.GetType().FullName,
                    ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Interprets a packet
        /// </summary>
        /// <param name="p">Packet</param>
        static void DataManager(Packet p)
        {
            try
            {
                switch (p.packetType)
                {
                    case PacketType.registration:
                        Controller.ConsoleMessage("Connected to Server",
                            ConsoleColor.DarkGray);
                        id = p.Gdata[0];
                        Packet P = new Packet(PacketType.registration, id);
                        p.Gdata.Add(Controller.Name);
                        p.Gdata.Add(Controller.ID.ToString());
                        master.Send(p.toBytes());
                        break;

                    case PacketType.chat:
                        Console.WriteLine(p.Gdata[0] + ": " + p.Gdata[1]);
                        break;

                    case PacketType.job:
                        if (p.packetJob.Args == "spell")
                        {
                            if (Controller.IsCasting)
                                JobResponse(p.packetJob.ID, "rejected", "busy");
                            else
                            {
                                JobResponse(p.packetJob.ID, "accepted");
                                Jobs.handler(p.packetJob);
                            }
                        }
                        else
                            if (Controller.IsBusy)
                                JobResponse(p.packetJob.ID, "rejected", "busy");
                            else
                            {
                                JobResponse(p.packetJob.ID, "accepted");
                                Jobs.handler(p.packetJob);
                            }
                        break;

                    default:
                        Controller.ConsoleMessage("The server sent a packet type I couldn't interpret",
                            ConsoleColor.Yellow);
                        break;
                }
            }
            catch (Exception x) //lazy, change later
            {
                Controller.ConsoleMessage(x.Message.ToString(),
                    ConsoleColor.Red);
                Controller.ConsoleMessage(x.GetType().FullName,
                    ConsoleColor.Red);
            }
        }
        
        /// <summary>
        /// Sends the server a message in regards to a job
        /// </summary>
        /// <param name="job">Data.Job object: represents a job</param>
        /// <param name="status">string: accepted, add, completed, rejected</param>
        public static void JobResponse(Job job, string status)
        {
            Packet p = new Packet(PacketType.job, id, job);
            p.Gdata.Add(Controller.Name);
            p.Gdata.Add(status);

            try
            {
                master.Send(p.toBytes());
            }
            catch (Exception e)
            {
                Controller.ConsoleMessage(e.Message.ToString(),
                    ConsoleColor.Red);
                Controller.ConsoleMessage(e.GetType().FullName,
                    ConsoleColor.Red);
            }
        }

        /// <summary>
        /// Sends the server a message in regards to a job
        /// </summary>
        /// <param name="jobID">guid: Job identifier</param>
        /// <param name="status">string: accepted, add, completed, rejected</param>
        /// <param name="reason">optional string: used for rejections</param>
        public static void JobResponse(Guid jobID, string status, string reason = null)
        {
            Packet p = new Packet(PacketType.job, id, jobID);
            p.Gdata.Add(Controller.Name);
            p.Gdata.Add(status);

            if (reason != null)
                p.Gdata.Add(reason);

            p.Gdata.Add(Controller.ID.ToString()); //server adds profile's id to job rejectors

            try
            {
                master.Send(p.toBytes());
            }
            catch (Exception x)
            {
                Controller.ConsoleMessage(x.Message.ToString(),
                    ConsoleColor.Red);
                Controller.ConsoleMessage(x.GetType().FullName,
                    ConsoleColor.Red);
            }
        }
    }
}
