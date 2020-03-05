using System;
using System.Linq;
using ScriptSDK;
using ScriptSDK.API;
using ScriptSDK.Mobiles;
using ScriptSDK.Items;
using Combot.Data;
using XScript.Items;

namespace Combot
{

    public class Jobs : Routines
    {
        /// <summary>
        /// Bandaging Job that the client is
        /// currently working on
        /// </summary>
        public static Job BandageJob;

        /// <summary>
        /// Routes incoming jobs to their proper
        /// methods
        /// </summary>
        /// <param name="job">object representing a job</param>
        public static void handler(Job job)
        {
            try
            {
                //Console.WriteLine("[{0}] Accepted job: {1}", Combot.combotName, job.ID);
                switch (job.Type)
                {
                    case "bandage":
                        BandageJob = job;
                        bandage(job);
                        break;

                    case "wounds":
                        wounds(job);
                        break;

                    case "cleanse":
                        cleanse(job);
                        break;

                    case "dispelevil":
                        dispelEvil(job);
                        break;

                    default:
                        Controller.ConsoleMessage("Handler for job type not found!",
                            ConsoleColor.Yellow);
                        Client.JobResponse(job.ID, "rejected", "no job handler");
                        break;
                }
            }
            catch (Exception x)
            {
                Controller.ConsoleMessage(x.Message.ToString(),
                    ConsoleColor.Red);
            }
        }

        private static void dispelEvil(Job job)
        {
            Mobile target = new Mobile(new Serial(job.TargetID));
            if (Controller.Self.Mana > 10 && target.Distance < 4)
            {
                Controller.IsCasting = true;
                Controller.ConsoleMessage("Casting Dispel Evil",
                    ConsoleColor.DarkGreen);
            A:
                Controller.Self.Cast("Dispel Evil");
                Stealth.Client.Wait(750);
                if (target.Valid)
                    goto A;
                else
                {
                    Controller.IsCasting = false;
                    Client.JobResponse(job.ID, "complete");
                    Controller.ConsoleMessage("Dispel Evil job complete",
                        ConsoleColor.Green);
                }
            }
            else
                Client.JobResponse(job.ID, "rejected", "target too far away");
        }

        private static void cleanse(Job job)
        {
            Mobile target = new Mobile(job.TargetID);
            if (Controller.Self.Mana > 7 && target.Distance < 12)
            {
                Controller.IsCasting = true;
                Controller.ConsoleMessage("Castring Cleanse by Fire on {0}",
                    ConsoleColor.DarkGreen, target.Name);
            A:
                Controller.Self.Cast("Cleanse by Fire", target.Serial.Value);
                Stealth.Client.Wait(750);
                if (target.Poisoned)
                    goto A;
                else
                {
                    Controller.IsCasting = false;
                    Client.JobResponse(job.ID, "complete");
                    Controller.ConsoleMessage("Cleanse job complete",
                        ConsoleColor.Green);
                }
            }
            else
                Client.JobResponse(job.ID, "rejected", "target too far away");
        }

        private static void wounds(Job job)
        {
            uint _targetID = job.TargetID;

            Mobile target = new Mobile(_targetID);
            if (target.Distance < 4 && Controller.Self.Mana > 7)
            {
                Controller.IsCasting = true;
                Controller.ConsoleMessage("Casting Close Wounds on {0}",
                    ConsoleColor.DarkGreen, _targetID);

                Controller.Self.Cast("Close Wounds", _targetID);
                Stealth.Client.Wait(2000);

                Controller.IsCasting = false;
                Client.JobResponse(job.ID, "complete");
                Controller.ConsoleMessage("Close Wounds job complete",
                    ConsoleColor.Green);
            }
            else
                Client.JobResponse(job.ID, "rejected", "target too far away");
        }

        private static void bandage(Job job)
        {
            Mobile _target = new Mobile(job.TargetID);
            var _bandages = Item.Find(typeof(Bandage), Controller.BackpackID, false);
            var _distance = _target.Distance;

            if (_distance < 2 && _target.Valid && _bandages.Count > 0)
            {
                if (_bandages.First().Amount <= 0)
                {
                    Controller.ConsoleMessage("Out of bandages!");
                    return;
                }

                Controller.IsBusy = true;
                Controller.ConsoleMessage("Bandaging {0}",
                    ConsoleColor.DarkGreen, job.TargetID);
            A:
                _bandages.First().DoubleClick();
                Stealth.Client.WaitTargetObject(job.TargetID);

                /* Instead of setting a script wait, we're doing
                 * something special for the bandage job.  We're
                 * using the Buff/Debuff system to catch when
                 * we start bandaging, and when we stop.
                 * */

                //if we didnt start bandaging and they are within range, then they're probably full HP
                Stealth.Client.Wait(150); //give it 150ms from using a bandage to checking if we used one
                if (!Combat.IsBandaging)
                {
                    if (_target.HealthPercent == 100)
                    {
                        Client.JobResponse(BandageJob.ID, "complete");
                        Controller.IsBusy = false;
                    }
                    //if we didn't start bandaging, we the target isn't 100%, then try again
                    else goto A;
                }
            }
            else if (_distance >= 2)
            {
                Client.JobResponse(job.ID, "rejected", "target too far away");
                Controller.IsBusy = false;
            }
            else if (_bandages.Count < 0)
            {
                Client.JobResponse(job.ID, "rejected", "out of bandages");
                Controller.IsBusy = false;
            }
        }
    }
}
