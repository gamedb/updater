using System;

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            
            Console.WriteLine(args);
            
            
            // Rollbar
            Log.setup();

            // Poll for new changes
            Steam.startChanges();

            // Consumers
            Rabbit.startConsumers();

            while (true)
            {
            }
        }
    }
}
