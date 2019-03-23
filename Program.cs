using System;
using System.Linq;
using System.Threading;
using Updater.Consumers;

namespace Updater
{
    internal static class ChangeFetcher
    {
        private static void Main(String[] args)
        {
            Console.Title = "GameDB Updater";

            // Config
            Config.init();

            // Wait for Rabbit
            AbstractConsumer.waitForConnections();

            // Poll for new changes
            Steam.startSteam(!args.Contains("--nopics"), false);
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));

                if (Steam.steamClient.IsConnected && Steam.isLoggedOn)
                {
                    break;
                }

                Log.Info("Waiting for Steam.. ");
            }

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