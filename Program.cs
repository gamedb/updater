using System;
using System.Threading;

namespace SteamUpdater
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            // Rollbar
            Log.setupRollbar();

            // Poll for new changes
            Steam.startSteam(false);

            // Consumers
            Consumers.AbstractConsumer.startConsumers();

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