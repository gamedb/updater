using System;
using Google.Api;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;
using Rollbar;

namespace Updater
{
    public static class Log
    {
        private static readonly LoggingServiceV2Client googleCLient = LoggingServiceV2Client.Create();

        public static void GoogleInfo(String message)
        {
            Console.WriteLine(message);

            var googleProject = Config.googleProject;
            var environment = Config.environment;

            var logName = new LogName(googleProject, environment + "-updater");
            var resource = new MonitoredResource {Type = "project"};
            var logEntry = new LogEntry
            {
                LogName = logName.ToString(),
                Severity = LogSeverity.Info,
                TextPayload = message
            };

            googleCLient.WriteLogEntries(LogNameOneof.From(logName), resource, null, new[] {logEntry});
        }

        public static void setupRollbar()
        {
            if (String.IsNullOrEmpty(Config.rollbarKey))
            {
                Console.WriteLine("Rollbar key environment variable not found");
            }
            else
            {
                var config = new RollbarConfig(Config.rollbarKey)
                {
                    Environment = Config.environment
                };

                RollbarLocator.RollbarInstance.Configure(config);
            }
        }

        public static void RollbarError(String message)
        {
            RollbarLocator.RollbarInstance.Error(message);
        }

        public static void RollbarInfo(String message)
        {
            RollbarLocator.RollbarInstance.Info(message);
        }
    }
}