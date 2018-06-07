using System;
using Rollbar;

namespace SteamProxy
{
    public static class Log
    {
        private const string key = "STEAM_PROXY_ROLLBAR_PRIVATE";
        private const string env = "ENV";

        public static void setup()
        {
            var rollbarKey = Environment.GetEnvironmentVariable(key);
            var environment = Environment.GetEnvironmentVariable(env);

            if (rollbarKey != "")
            {
                var config = new RollbarConfig(rollbarKey)
                {
                    Environment = environment
                };

                RollbarLocator.RollbarInstance.Configure(config);
            }
        }

        public static void Info(string message)
        {
            RollbarLocator.RollbarInstance.Info(message);
        }
    }
}