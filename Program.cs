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

            // Rollbar
            Log.setupRollbar();

            // Wait for Rabbit
            while (true)
            {
                try
                {
                    var conn = AbstractConsumer.getConnection();
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Waiting for Rabbit.. " + ex.Message);
                }
                finally
                {
                    conn.Close();
                    conn.Dispose();
                }

                Thread.Sleep(TimeSpan.FromSeconds(1));
                break;
            }

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