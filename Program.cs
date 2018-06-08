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
            Steam.startSteam();

            // Consumers
            Rabbit.startConsumers();

            // Block thread
            Thread.Sleep(Timeout.Infinite);
        }
    }
}