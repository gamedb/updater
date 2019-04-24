using System;
using Google.Api;
using Google.Cloud.Logging.Type;
using Google.Cloud.Logging.V2;

namespace Updater
{
    public static class Log
    {
        private static readonly LoggingServiceV2Client googleCLient = LoggingServiceV2Client.Create();

        public static void Debug(String message)
        {
            log(message, LogSeverity.Debug);
        }

        public static void Info(String message)
        {
            log(message, LogSeverity.Info);
        }

        public static void Error(String message)
        {
            log(message, LogSeverity.Error);
        }

        public static void Critical(String message)
        {
            log(message, LogSeverity.Critical);
        }

        private static void log(String message, LogSeverity severity)
        {
            message = $"{DateTime.Now:HH:mm:ss} {severity} - {message}";

            Console.WriteLine(message);

            if (!Config.isLocal())
            {
                var logName = new LogName(Config.googleProject, Config.environment + "-updater");
                var resource = new MonitoredResource {Type = "project"};
                var logEntry = new LogEntry
                {
                    LogName = logName.ToString(),
                    Severity = severity,
                    TextPayload = message
                };

                logEntry.Labels.Add("env", Config.environment);

                googleCLient.WriteLogEntries(LogNameOneof.From(logName), resource, null, new[] {logEntry});
            }
        }
    }
}