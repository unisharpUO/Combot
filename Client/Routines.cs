using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Combot
{
    /// <summary>
    /// Routines is a class that controls all the
    /// routines that the client needs to run
    /// (e.g. Combat, Looting, Dress, etc.) 
    /// </summary>
    public class Routines
    {
        public static void Start()
        {
            //add your routine here
            ParameterizedThreadStart _startLootThread = new ParameterizedThreadStart(LootRoutine.StartRoutine);
            var _lootThread = new Thread(_startLootThread);
            _lootThread.Start();
            
            //main thread goes here and stays there, do not edit
            Combat.StartRoutine();
        }
    }
}
