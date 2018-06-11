using System;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public abstract class AbstractConsumer
    {
        // Consts
        public const string queueAppId = "App_ID";
        public const string queueAppData = "App_Data";
        public const string queuePackageId = "Package_ID";
        public const string queuePackageData = "Package_Data";

        private const string append = "Steam_Updater_";

        // Abstracts
        protected abstract void HandleMessage(BasicDeliverEventArgs msg);

        // Statics
        public static void startConsumers()
        {
            var consumers = new Dictionary<string, AbstractConsumer>
            {
                {queueAppId, new AppIDsConsumer()},
                {queuePackageId, new PackageIDsConsumer()}
            };

            foreach (var entry in consumers)
            {
                var thread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    var consumer = entry.Value;
                    consumer.Consume(append + entry.Key);
                });

                thread.Start();
            }
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

        //
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

        private void Consume(string queue)
        {
            Console.WriteLine("Consuming " + queue);

            var x = getConnection();
            var connection = x.Item1;
            var channel = x.Item2;

            connection.ConnectionShutdown += (s, e) =>
            {
                connection.Dispose();
                Consume(queue);
            };

            channel.QueueDeclare(queue, true, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += delegate(object model, BasicDeliverEventArgs ea) { HandleMessage(ea); };

            channel.BasicConsume(queue, true, consumer);
        }
    }
}