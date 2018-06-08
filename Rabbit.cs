using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

namespace SteamProxy
{
    public static class Rabbit
    {
        public const string queueAppIds = "app-ids";
        public const string queueAppDatas = "app-datas";
        public const string queuePackageIds = "package-ids";
        public const string queuePackageDatas = "package-datas";

        private const string append = "steam-proxy-";

        public static void startConsumers()
        {
            var consumers = new Dictionary<string, Func<BasicDeliverEventArgs, bool>>
            {
                {queueAppIds, ConsumeAppID},
                {queuePackageIds, ConsumePackageID}
            };

            foreach (var entry in consumers)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    Consume(entry.Key, entry.Value);
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

            connection.ConnectionShutdown += (s, e) => { connection.Dispose(); };

            channel.QueueDeclare(queue, true, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) => { callback(ea); };

            channel.BasicConsume(queue, true, consumer);
        }

        private static bool ConsumeAppID(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            Steam.steamApps.PICSGetProductInfo(Convert.ToUInt32(msgBody), null, false);
            return true;
        }

        private static bool ConsumePackageID(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            Steam.steamApps.PICSGetProductInfo(null, Convert.ToUInt32(msgBody), false);
            return true;
        }
    }
}