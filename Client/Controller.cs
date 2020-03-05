using System;
using System.Threading;
using ScriptSDK.API;
using ScriptSDK.Mobiles;

namespace Combot
{
    using Data;
    using ScriptSDK.Engines;

    public class Controller
    {
        #region Properties
        public static PlayerMobile Self = PlayerMobile.GetPlayer();
        private static bool _IsCasting;
        private static bool _IsBusy;
        public static bool IsCasting
        {
            get { return _IsCasting; }
            set
            {
                _IsCasting = value;

                //Let the server know if we're casting
                Packet p = new Packet(PacketType.status, Client.id, value);
                p.Gdata.Add(Controller.Name);
                p.Gdata.Add("IsCasting");

                try
                {
                    Client.master.Send(p.toBytes());
                }
                catch (Exception e)
                {
                    Controller.ConsoleMessage(e.Message.ToString(),
                        ConsoleColor.Red);
                }
            }
        }
        public static bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                _IsBusy = value;

                //Let the server know we're busy
                Packet p = new Packet(PacketType.status, Client.id, value);
                p.Gdata.Add(Controller.Name);
                p.Gdata.Add("IsBusy");
                try
                {
                    Client.master.Send(p.toBytes());
                }
                catch (Exception e)
                {
                    Controller.ConsoleMessage(e.Message.ToString(),
                        ConsoleColor.Red);
                }
            }
        }
        public static readonly object UseLock = new object();
        public static uint ID = Self.Serial.Value;
        public static uint PlayerID = Self.Serial.Value;
        public static uint BackpackID = Self.Backpack.Serial.Value;
        public static string Name = Self.Name;
        #endregion

        /// <summary>
        /// Program start
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        public static void Main(string[] args)
        {
        A:
            if (Stealth.Client.GetConnectedStatus())
            {
                Controller.ConsoleMessage("Connected to Stealth UO profile",
                    ConsoleColor.DarkGray);
                Controller.ConsoleMessage("Attempting to connect to server...",
                    ConsoleColor.DarkGray);
                try
                {
                    Self.Backpack.DoubleClick();
                    Thread.Sleep(1000);
                    Client.Connect();

                    /* * *
                     * Stealth Global Settings/Variables
                     * * * * * */
                    Stealth.Client.SetMoveThroughNPC(0);
                    Scanner.Initialize();

                    Routines.Start();
                }
                catch (Exception x)
                {
                    Console.WriteLine(x.Message.ToString());
                }
            }
            else
            {
                Console.WriteLine("**Selected profile is not online, retrying in 5 seconds**");
                Thread.Sleep(5000);
                goto A;
            }
        }

        /// <summary>
        /// Method for posting messages to the console
        /// with a timestamp
        /// </summary>
        /// <param name="message">string: message to send to the console</param>
        /// <param name="args">(optional) object[]: additional arguments</param>
        public static void ConsoleMessage(string message, params object[] args)
        {
            Console.Write("[{0}-{1}] ", Controller.Name, DateTime.Now.ToString("mm:ss"));
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
            Console.Write("[{0}-{1}] ", Controller.Name, DateTime.Now.ToString("mm:ss"));
            Console.ForegroundColor = color;
            Console.WriteLine(message, args);
            Console.ResetColor();
        }
    }
}
