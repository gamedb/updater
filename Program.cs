using System;
using System.Threading;
using Newtonsoft.Json;
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

            // Rollbar
            Log.setupRollbar();

            // Wait for Rabbit
            while (true)
            {
                try
                {
                    AbstractConsumer.getProducerConnection();
                    AbstractConsumer.getConsumerConnection();
                    break;
                }
                catch (Exception ex)
                {
                    Log.GoogleInfo("Waiting for Rabbit.. " + ex.Message + " - " + ex.InnerException.Message);
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            // Poll for new changes
            Steam.startSteam(false);
            while (true)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));

                if (Steam.steamClient.IsConnected && Steam.isLoggedOn)
                {
                    break;
                }

                Log.GoogleInfo("Waiting for Steam.. ");
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