using System;
using System.Threading;

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            // Rollbar
            Log.setup();

            // Poll for new changes
            Steam.startSteam(false);

            // Consumers
            Rabbit.startConsumers();

            // On quit
            Console.CancelKeyPress += delegate
            {
                Steam.quitOnDisconnect = true;
                Steam.steamUser.LogOff();
                Thread.Sleep(Timeout.Infinite);
            };

            // Block thread
            Thread.Sleep(Timeout.Infinite);
        }
    }
}