using System;
using System.Collections.Generic;
using System.Linq;
using ScriptSDK;
using ScriptSDK.API;
using ScriptSDK.Mobiles;

namespace Combot.Routines
{
    using Server;
    using Data;

    /// <summary>
    /// Combat is a routine to handle healing
    /// and attacking
    /// </summary>
    public class Combat : Combot.Routine
    {
        #region Properties
        /// <summary>
        /// Basic list of creatures used for
        /// checking health in the Combat Routine
        /// </summary>
        static List<Mobile> GroupList = new List<Mobile>
        {
            new Mobile(new Serial(8439902)), //Sad
            //new Creature(37928828), //Splen
            //new Creature(16373319), //Tor
            //new Creature(3985473), //Wil
            new Mobile(new Serial(11914331)), //Cai
            new Mobile(new Serial(13342484)), //Sad
            new Mobile(new Serial(39645748)) //Aut
        };

        /// <summary>
        /// ActiveGroupList is a changing property that
        /// consists of Creatures in the GroupList which
        /// are isExist
        /// </summary>
        public static List<Mobile> ActiveGroupList;
        #endregion

        /// <summary>
        /// Main routine loop for generating in-game
        /// data to send to the clients
        /// </summary>
        public static void routine()
        {
            //int count = 0;
            Combot.ConsoleMessage("Combat routine started...",
                ConsoleColor.DarkGray);
            while (Stealth.Client.GetConnectedStatus())
            {
                #region Health Check
                ActiveGroupList = GroupList.Where(x => x.Valid).OrderBy(x => x.HealthPercent).ToList();
                foreach (Mobile _player in ActiveGroupList)
                {
                    var _playerID = _player.Serial.Value;
                    var _currentHealth = _player.HealthPercent;
                    if (_player.Distance < 10)
                    {
                        if (_player.Poisoned)
                            Server.jobQueue.enqueue(new Job("cleanse", _playerID, 10, "spell"));
                        if (_currentHealth < 60)
                            Server.jobQueue.enqueue(new Job("wounds", _playerID, 30, "spell"));
                        if (_currentHealth < 99)
                            Server.jobQueue.enqueue(new Job("bandage", _playerID, 50));
                    }
                }
                #endregion

                #region Debugging Jobs
                /*
                Server.jobQueue.enqueue(new Job("bandage", GroupList[4].ID, 50));
                Server.jobQueue.enqueue(new Job("bandage", GroupList[5].ID, 50));
                Server.jobQueue.enqueue(new Job("bandage", GroupList[6].ID, 50));


                Server.jobQueue.enqueue(new Job("cleanse", GroupList[5].ID, 10, "spell"));
                Server.jobQueue.enqueue(new Job("wounds", GroupList[6].ID, 30, "spell"));
                 * */
                #endregion

                Stealth.Client.Wait(1000); // Slow down the requests to Stealth, maybe turn back up after changes
                //Main thread loops infinitely
            }
        }
    }
}
