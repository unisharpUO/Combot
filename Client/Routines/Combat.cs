using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StealthAPI;
using ScriptSDK;
using ScriptSDK.Engines;
using ScriptSDK.Items;
using ScriptSDK.Mobiles;
using XScript.Items;

namespace Combot
{

    public class Combat : Routines
    {
        public static bool AutoFollow = false, PrimaryAttack = false, IsBandaging = false;
        public static Mobile Leader = new Mobile(new Serial(8439902));
        public static uint LeaderID = 8439902;

        public static void StartRoutine()
        {
            Scanner.Initialize();

            #region Events
            Stealth.Client.Buff_DebuffSystem += onBuff;
            Stealth.Client.UnicodeSpeech += onSpeech;
            Stealth.Client.PartyInvite += onInvite;
            Stealth.Client.ClilocSpeech += onClilocSpeech;
            #endregion Events

            Stopwatch _abilityClock = new Stopwatch();

            _abilityClock.Start();

            //Do stuff in here that you want the client to do on its own
            while (Stealth.Client.GetConnectedStatus())
            {
                /* *
                 * Client Combat Routine Methods Go Here
                 * * */
                var _myMana = Controller.Self.Mana;
                
                #region Movement Check

                if (AutoFollow)
                {
                    var _leaderX = Leader.Location.X;
                    var _leaderY = Leader.Location.Y;

                    if (Controller.Self.Location.X != _leaderX || Controller.Self.Location.Y != _leaderY)
                        Stealth.Client.MoveXY((ushort)_leaderX, (ushort)_leaderY, false, 1, true);
                }

                #endregion

                if (Stealth.Client.GetAbility() == "0" && _myMana > 30)
                    Stealth.Client.UsePrimaryAbility();

                Stealth.Client.Wait(1000); //only do client checks every so often
            }
        }

        static void onInvite(object sender, PartyInviteEventArgs e)
        {
            if (e.InviterId == LeaderID)
                Stealth.Client.PartyAcceptInvite();
        }

        static void onSpeech(object sender, UnicodeSpeechEventArgs e)
        {
            if (e.SenderId == Leader.Serial.Value)
            {
                switch (e.Text)
                {
                    case "PartyPrivateMsg: drop party":
                        Stealth.Client.PartyLeave();
                        break;

                    case "PartyPrivateMsg: clear scanner":
                        Scanner.ClearIgnoreList();
                        Controller.ConsoleMessage("Clearing Scanner", ConsoleColor.Gray);
                        Stealth.Client.PartySay("Clearing Scanner");
                        break;

                    case "PartyPrivateMsg: bandages":
                        Controller.Self.Backpack.DoubleClick();
                        Stealth.Client.Wait(500);
                        var _bandages = Item.Find(typeof(Bandage), Controller.BackpackID, false);

                        if (_bandages.Count > 0)
                            Stealth.Client.PartySay("I have " + _bandages.First().Amount + " bandages left.");
                        else
                            Stealth.Client.PartySay("I'm out of bandages!");

                        break;

                    case "PartyPrivateMsg: follow":
                        Stealth.Client.MoveXY((ushort)Leader.Location.X, (ushort)Leader.Location.Y, false, 0, true);
                        break;

                    case "PartyPrivateMsg: follow toggle":
                        AutoFollow = !AutoFollow;
                        Controller.ConsoleMessage("AutoFollow {0}", ConsoleColor.Gray, AutoFollow.ToString());
                        Stealth.Client.PartySay("AutoFollow " + AutoFollow.ToString());
                        break;

                    case "PartyPrivateMsg: confirm trade":
                        Stealth.Client.ConfirmTrade(0);
                        break;

                    case "PartyPrivateMsg: eoo":
                        Controller.Self.Cast("Enemy of One");
                        break;

                    case "PartyPrivateMsg: use ladder":
                        if (Stealth.Client.IsObjectExists(1073868310))
                        {
                            Stealth.Client.SetMoveThroughNPC(0);
                            Stealth.Client.MoveXY(6432, 1699, false, 0, true);
                            Stealth.Client.UseObject(1073868310);
                        }
                        else if (Stealth.Client.IsObjectExists(1073869466))
                        {
                            Stealth.Client.SetMoveThroughNPC(0);
                            Stealth.Client.MoveXY(6305, 1672, false, 0, true);
                            Stealth.Client.UseObject(1073869466);
                        }
                        else
                            Stealth.Client.PartySay("couldn't find ladder");
                        break;

                    case "PartyPrivateMsg: use rope ladder":
                        if (Stealth.Client.IsObjectExists(1073868317))
                        {
                            Stealth.Client.SetMoveThroughNPC(0);
                            Stealth.Client.MoveXY(6432, 1633, false, 0, true);
                            Stealth.Client.UseObject(1073868317);
                        }
                        else if (Stealth.Client.IsObjectExists(1073869466))
                        {
                            Stealth.Client.SetMoveThroughNPC(0);
                            Stealth.Client.MoveXY(6305, 1672, false, 0, true);
                            Stealth.Client.UseObject(1073869466);
                        }
                        else
                            Stealth.Client.PartySay("couldn't find rope ladder");
                        break;

                    case "PartyPrivateMsg: step down":
                        Stealth.Client.Step(0, false);
                        break;

                    case "PartyPrivateMsg: toggle primary":
                        PrimaryAttack = !PrimaryAttack;
                        Controller.ConsoleMessage("PrimaryAttack {0}", ConsoleColor.Gray, PrimaryAttack.ToString());
                        Stealth.Client.PartySay("PrimaryAttack " + PrimaryAttack.ToString());
                        break;

                    case "PartyPrivateMsg: help":
                        Stealth.Client.PartySay("Commands are: drop party, bandages, follow, follow toggle, confirm trade, eoo, use ladder, use rope ladder, step down");
                        break;

                    default:
                        break;
                }
            }
        }

        private static void onClilocSpeech(object sender, ClilocSpeechEventArgs e)
        {
            switch (e.Text)
            {
                case "That being is not damaged!":
                    IsBandaging = false;
                    break;

                default:
                    break;
            }
        }

        static void onBuff(object sender, Buff_DebuffSystemEventArgs e)
        {
            if (e.AttributeId == 1038)
            {
                Data.Job j = new Data.Job("cleanse", Controller.ID, 10, "spell");
                Client.JobResponse(j, "add");
            }
            /* * 
             * 1069 is a buff that comes up 3 times whenever
             * a bandage is being used.  So we're using that
             * to determine whether we're bandaging or not.
             * * */
            if (e.AttributeId == 1069)
            {
                if (!IsBandaging)
                {
                    IsBandaging = true;
                    Controller.IsBusy = true;
                }
            }
            else if (e.AttributeId == 1101) //1101 means we finished bandaging
            {
                Controller.ConsoleMessage("Caught a 1101 Debuff Event",
                    ConsoleColor.DarkYellow);

                /* *
                 * We have to set the bools to false before telling
                 * the server that we completed the job so the server
                 * doesn't try giving us a job while we're still busy
                 * * */
                IsBandaging = false;
                Controller.IsBusy = false;
                Client.JobResponse(Jobs.BandageJob.ID, "complete");
                Controller.ConsoleMessage("Bandage job complete",
                        ConsoleColor.Green);
            }
        }
    }
}
