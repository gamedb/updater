using System;
using System.Timers;
using SteamProxy.Consumers;
using System.Threading.Tasks; 

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            // Rollbar
            Log.Setup();

            // PICS
            Steam.startChanges();

            // Consumers
            Rabbit.startConsumers();

            while (true)
            {
            }
        }
    }
}