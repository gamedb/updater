using System;
using System.Threading;
using Updater.Consumers;

namespace Updater
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            Console.Title = "Game DB Updater";

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
                    Console.WriteLine("Waiting for Rabbit.. " + ex.Message + " - " + ex.InnerException.Message);
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
            };

            // Block thread
            Thread.Sleep(Timeout.Infinite);
        }
    }
}