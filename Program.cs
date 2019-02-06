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