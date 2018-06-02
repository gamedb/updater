using System;
using System.Timers;

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        public static Steam steam;

        private static void Main(string[] args)
        {
            // Rollbar
            Log.Setup();

            //
            steam = new Steam();
            steam.Connect();

            var timer1 = new Timer();
            timer1.Elapsed += steam.CheckForChanges;
            timer1.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            timer1.Start();

            var timer2 = new Timer();
            timer2.Elapsed += steam.RunWaitCallbacks;
            timer2.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            timer2.Start();

            while (true)
            {
            }
        }
    }
}