using System;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public abstract class AbstractConsumer
    {
        // Consts
        public const string queueApps = "Apps";
        public const string queueAppsData = "Apps_Data";
        public const string queuePackages = "Packages";
        public const string queuePackagesData = "Packages_Data";
        public const string queueProfiles = "Profiles";
        public const string queueProfilesData = "Profiles_Data";
        public const string queueChangesData = "Changes_Data";

        private const string queueAppend = "Steam_";

        private static readonly ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST"),
            UserName = Environment.GetEnvironmentVariable("STEAM_RABBIT_USER"),
            Password = Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS")
        };

        // Abstracts
        protected abstract Task<bool> HandleMessage(BasicDeliverEventArgs msg);

        // Statics
        public static void startConsumers()
        {
            var consumers = new Dictionary<string, AbstractConsumer>
            {
                {queueApps, new AppConsumer()},
                {queuePackages, new PackageConsumer()},
                {queueProfiles, new ProfileConsumer()}
            };

            foreach (var entry in consumers)
            {
                var thread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    var consumer = entry.Value;
                    consumer.Consume(queueAppend + entry.Key);
                });

                thread.Start();
            }
        }

        private static (IConnection, IModel) getConnection()
        {
            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            return (connection, channel);
        }

        public static void Produce(string queue, string data)
        {
            var x = getConnection();
            var connection = x.Item1;
            var channel = x.Item2;

            channel.QueueDeclare(queueAppend + queue, true, false, false);

            var bytes = Encoding.UTF8.GetBytes(data);
            channel.BasicPublish("", queueAppend + queue, null, bytes);

            channel.Close();
            connection.Close();
        }

        private void Consume(string queue)
        {
            Log.GoogleInfo("Consuming " + queue);

            var x = getConnection();
            var connection = x.Item1;
            var channel = x.Item2;

            connection.ConnectionShutdown += (s, e) =>
            {
                connection.Dispose();
                Consume(queue);
            };

            channel.QueueDeclare(queue, true, false, false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += delegate(object chan, BasicDeliverEventArgs ea)
            {
                var success = HandleMessage(ea);
                if (success.Result)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            channel.BasicConsume(queue, true, consumer);
        }
    }
}