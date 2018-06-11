using System;
using Rollbar;
using Google.Cloud.Logging.V2;
using Google.Cloud.Logging.Type;
using Google.Api;

namespace SteamUpdater
{
    public static class Log
    {
        private const string key = "STEAM_PROXY_ROLLBAR_PRIVATE";
        private const string env = "ENV";

        private static readonly LoggingServiceV2Client googleCLient = LoggingServiceV2Client.Create();

        public static void setupRollbar()
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

        public static void RollbarError(string message)
        {
            RollbarLocator.RollbarInstance.Error(message);
        }

        public static void RollbarInfo(string message)
        {
            RollbarLocator.RollbarInstance.Info(message);
        }

        public static void GoogleInfo(string message)
        {
            Console.WriteLine(message);

            var googleProject = Environment.GetEnvironmentVariable("STEAM_GOOGLE_PROJECT");
            var envi = Environment.GetEnvironmentVariable("ENV");

            var logName = new LogName(googleProject, "steam-updater-" + envi);
            var logEntry = new LogEntry
            {
                LogName = logName.ToString(),
                Severity = LogSeverity.Info,
                TextPayload = message
            };
            var resource = new MonitoredResource {Type = "global"};

            googleCLient.WriteLogEntries(LogNameOneof.From(logName), resource, null, new[] {logEntry});
        }
    }
}