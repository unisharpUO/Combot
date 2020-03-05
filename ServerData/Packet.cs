using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace Combot.Data
{
    using Combot;

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public class Packet
    {
        #region Properties
        public List<string> Gdata;
        public int packetInt;
        public bool packetBool;
        public string senderId;
        public PacketType packetType;
        public Job packetJob;
        public Guid packetJobID;
        public bool status;
        #endregion

        #region Methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

        }

        /// <summary>
        /// Packet containing only a list of strings
        /// </summary>
        /// <param name="type">Type of packet</param>
        /// <param name="senderId">ID of sender</param>
        public Packet(PacketType type, string senderId)
        {
            Gdata = new List<string>();
            this.senderId = senderId;
            this.packetType = type;
        }

        /// <summary>
        /// Packet containing a list of strings and a Job object
        /// </summary>
        /// <param name="type">Type of packet</param>
        /// <param name="senderId">ID of sender</param>
        /// <param name="job">An instance of the Job object</param>
        public Packet(PacketType type, string senderId, Job job)
        {
            Gdata = new List<string>();
            this.senderId = senderId;
            this.packetType = type;
            this.packetJob = job;
        }

        /// <summary>
        /// Packet containing a list of strings and a Job ID
        /// </summary>
        /// <param name="type">Type of packet</param>
        /// <param name="senderId">ID of sender</param>
        /// <param name="jobID">ID of the job</param>
        public Packet(PacketType type, string senderId, Guid jobID)
        {
            Gdata = new List<string>();
            this.senderId = senderId;
            this.packetType = type;
            this.packetJobID = jobID;
        }

        public Packet(PacketType type, string senderId, bool status)
        {
            Gdata = new List<string>();
            this.senderId = senderId;
            this.packetType = type;
            this.status = status;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="packetBytes"></param>
        public Packet(byte[] packetBytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(packetBytes);

            try
            {
                Packet p = (Packet)bf.Deserialize(ms);
                ms.Close();
                Gdata = p.Gdata;
                packetInt = p.packetInt;
                packetBool = p.packetBool;
                senderId = p.senderId;
                packetType = p.packetType;
                packetJob = p.packetJob;
                packetJobID = p.packetJobID;
                status = p.status;
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message.ToString());
                Console.WriteLine(x.GetType().FullName);
            }
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] toBytes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, this);
            byte[] bytes = ms.ToArray();
            ms.Close();
            return bytes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static string getIp4Address()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress i in ips)
            {
                if (i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return i.ToString();
                }
            }
            return "127.0.0.1";
        }
        #endregion
    }

    /// <summary>
    /// Defines the type of packet
    /// </summary>
    public enum PacketType
    {
        chat,
        job,
        registration,
        status
    }

    /// <summary>
    /// Object that is used to store information
    /// about a task for a client to do
    /// </summary>
    [Serializable]
    public class Job : IComparable<Job>
    {
        #region Properties
        private Guid _id;
        private string _type;
        private uint _targetID;
        private double _priority;
        private string _args;
        private bool _assigned;
        private DateTime _created;

        /// <summary>
        /// Job identifier
        /// </summary>
        public Guid ID
        {
            get { return _id; }
            set { _id = value; }
        }

        public List<uint> Rejectors;

        /// <summary>
        /// Type of job (bandage, cleanse, etc.)
        /// </summary>
        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// ID of the target
        /// </summary>
        public uint TargetID
        {
            get { return _targetID; }
            set { _targetID = value; }
        }

        /// <summary>
        /// Smaller value means higher priority
        /// </summary>
        public double Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }

        /// <summary>
        /// Additional custom arguments for the job
        /// </summary>
        public string Args
        {
            get { return _args; }
            set { _args = value; }
        }

        /// <summary>
        /// Is the job assigned to a client
        /// </summary>
        public bool Assigned
        {
            get { return _assigned; }
            set { _assigned = value; }
        }

        public DateTime Created
        {
            get { return _created; }
            set { _created = value; }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Object representing a task for a client to accomplish
        /// </summary>
        /// <param name="Type">bandage, cleanse, dispel, wounds</param>
        /// <param name="TargetID">ID of the target</param>
        /// <param name="Priority">smaller values are higher priority</param>
        public Job(string Type, uint TargetID, double Priority)
        {
            this.ID = Guid.NewGuid();
            this.Type = Type;
            this.TargetID = TargetID;
            this.Priority = Priority;
            this.Rejectors = new List<uint>();
            this.Args = "";
            this.Assigned = false;
            this.Created = DateTime.Now;
        }

        /// <summary>
        /// Object representing a task for a client to accomplish
        /// </summary>
        /// <param name="Type">bandage, cleanse, dispel, wounds</param>
        /// <param name="TargetID">ID of the target</param>
        /// <param name="Args">any other instructions to pass to client</param>
        /// <param name="Priority">smaller values are higher priority</param>
        public Job(string Type, uint TargetID, double Priority, string Args)
        {
            this.ID = Guid.NewGuid();
            this.Type = Type;
            this.TargetID = TargetID;
            this.Priority = Priority;
            this.Rejectors = new List<uint>();
            this.Args = Args;
            this.Assigned = false;
            this.Created = DateTime.Now;
        }

        /// <summary>
        /// Returns the Job object as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Type + "," + TargetID.ToString() + "," + Priority.ToString() + "," + Args + "," + Assigned.ToString();
        }

        /// <summary>
        /// Used to compare priorities
        /// </summary>
        /// <param name="other">Job to compare priorities with</param>
        /// <returns></returns>
        public int CompareTo(Job other)
        {
            if (this.Priority < other.Priority) return -1;
            else if (this.Priority > other.Priority) return 1;
            else return 0;
        }
        #endregion
    }

}