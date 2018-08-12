using System;
using System.Threading;
using SteamUpdater.Consumers;

namespace SteamUpdater
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            Console.Title = "Steam Updater";

            // Google
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                Environment.GetEnvironmentVariable("STEAM_GOOGLE_APPLICATION_CREDENTIALS")
            );

            // Rollbar
            Log.setupRollbar();

            // Wait for Rabbit
            while (true)
            {
                try
                {
                    AbstractConsumer.connect();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Waiting for Rabbit.. " + ex.Message);
                }

                Thread.Sleep(TimeSpan.FromSeconds(2));
            }

            // Poll for new changes
            Steam.startSteam(false);

            // Consumers
            AbstractConsumer.startConsumers();

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