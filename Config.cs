using System;

namespace Updater
{
    public static class Config
    {
        // Steam account
        public static String steamUsername;
        public static String steamPassword;

        // Rabbit
        public static String rabbitUsername;
        public static String rabbitPassword;
        public static String rabbitHostname;
        public static Int32 rabbitPort;

        // Other
        public static String googleProject;
        public static String environment;
        public static String rollbarKey;
        public static String slackWebhook;

        public static void init()
        {
            // Steam account
            steamUsername = Environment.GetEnvironmentVariable("STEAM_PROXY_USERNAME");
            steamPassword = Environment.GetEnvironmentVariable("STEAM_PROXY_PASSWORD");

            // Rabbit
            rabbitUsername = Environment.GetEnvironmentVariable("STEAM_RABBIT_USER");
            rabbitPassword = Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS");
            rabbitHostname = Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST");
            rabbitPort = Int32.Parse(Environment.GetEnvironmentVariable("STEAM_RABBIT_PORT"));

            // Other
            googleProject = Environment.GetEnvironmentVariable("STEAM_GOOGLE_PROJECT");
            environment = Environment.GetEnvironmentVariable("STEAM_ENV");
            rollbarKey = Environment.GetEnvironmentVariable("STEAM_PROXY_ROLLBAR_PRIVATE");
            slackWebhook = Environment.GetEnvironmentVariable("STEAM_PROXY_SLACK_WEBHOOK");
        }

        public static Boolean isLocal()
        {
            return environment == "local";
        }
    }
}