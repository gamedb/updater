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

        private static void log(String message, LogSeverity severity)
        {
            message = $"{DateTime.Now:hh:mm:ss} - {severity} - {message}";

            Console.WriteLine(message);

            var logName = new LogName(Config.googleProject, Config.environment + "-updater");
            var resource = new MonitoredResource {Type = "project"};
            var logEntry = new LogEntry
            {
                LogName = logName.ToString(),
                Severity = severity,
                TextPayload = message
            };

            googleCLient.WriteLogEntries(LogNameOneof.From(logName), resource, null, new[] {logEntry});
        }
    }
}