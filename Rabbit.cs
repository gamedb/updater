using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace SteamUpdater
{
    public static class Rabbit
    {
        // Queue names
        public const string queueAppId = "App_ID";
        public const string queueAppData = "App_Data";
        public const string queuePackageId = "Package_ID";
        public const string queuePackageData = "Package_Data";

        private const string append = "Steam_Updater_";

        public static void startConsumers()
        {
            var consumers = new Dictionary<string, Func<BasicDeliverEventArgs, bool>>
            {
                {queueAppId, ConsumeAppID},
                {queuePackageId, ConsumePackageID}
            };

            foreach (var entry in consumers)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    Consume(append + entry.Key, entry.Value);
                }).Start();
            }
        }

        private static (IConnection, IModel) getConnection()
        {
            var factory = new ConnectionFactory
            {
                HostName = Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST"),
                UserName = Environment.GetEnvironmentVariable("STEAM_RABBIT_USER"),
                Password = Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS")
            };

            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            return (connection, channel);
        }

        public static void Produce(string queue, string data)
        {
            var x = getConnection();
            var connection = x.Item1;
            var channel = x.Item2;

            channel.QueueDeclare(append + queue, true, false, false, null);

            var bytes = Encoding.UTF8.GetBytes(data);
            channel.BasicPublish("", append + queue, null, bytes);

            channel.Close();
            connection.Close();
        }

        private static void Consume(string queue, Func<BasicDeliverEventArgs, bool> callback)
        {
            Console.WriteLine("Consuming " + queue);

            var x = getConnection();
            var connection = x.Item1;
            var channel = x.Item2;

            connection.ConnectionShutdown += (s, e) =>
            {
                connection.Dispose();
                Consume(queue, callback);
            };

            channel.QueueDeclare(queue, true, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += delegate(object model, BasicDeliverEventArgs ea) { callback(ea); };

            channel.BasicConsume(queue, true, consumer);
        }

        private static bool ConsumeAppID(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");
            if (ids.Length > 0)
            {
                var idInts = Array.ConvertAll(ids, Convert.ToUInt32);
                Steam.steamApps.PICSGetProductInfo(idInts, null, false);
            }

            return true;
        }

        private static bool ConsumePackageID(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");
            if (ids.Length > 0)
            {
                var idInts = Array.ConvertAll(ids, Convert.ToUInt32);
                Steam.steamApps.PICSGetProductInfo(null, idInts, false);
            }

            return true;
        }
    }
}