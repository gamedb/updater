using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Updater.Consumers
{
    public abstract class AbstractConsumer
    {
        private static IConnection connection;
        private static IModel channel;

        public const String queueApps = "Apps";
        protected const String queueAppsData = "Apps_Data";
        public const String queuePackages = "Packages";
        protected const String queuePackagesData = "Packages_Data";
        public const String queueProfiles = "Profiles";
        protected const String queueProfilesData = "Profiles_Data";
        public const String queueChangesData = "Changes_Data";
        private const String queueAppend = "Steam_";

        // Queue -> Consumer
        public static readonly Dictionary<String, AbstractConsumer> consumers = new Dictionary<String, AbstractConsumer>
        {
            {queueApps, new AppConsumer()},
            {queuePackages, new PackageConsumer()},
            {queueProfiles, new ProfileConsumer()}
        };

        //
        private static readonly ConnectionFactory connectionFactory = new ConnectionFactory
        {
            UserName = Environment.GetEnvironmentVariable("STEAM_RABBIT_USER"),
            Password = Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS"),
            HostName = Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST"),
            Port = Int32.Parse(Environment.GetEnvironmentVariable("STEAM_RABBIT_PORT"))
        };

        // Abstract
        protected abstract Task<Tuple<Boolean, Boolean>> HandleMessage(BasicDeliverEventArgs msg);

        // Statics
        public static void startConsumers()
        {
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

        public static void Produce(String queue, String data)
        {
            if (data.Length == 0)
            {
                return;
            }

            try
            {
                connect();

                channel.QueueDeclare(queueAppend + queue, true, false, false);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                var bytes = Encoding.UTF8.GetBytes(data);
                channel.BasicPublish("", queueAppend + queue, properties, bytes);
            }
            catch (Exception ex)
            {
                Log.RollbarError("Failed producing to " + queue + " with data: " + data + " - " + ex.Message);
            }
        }

        private void Consume(String queue)
        {
            Log.GoogleInfo("Consuming " + queue);

            connect();

            connection.ConnectionShutdown += (s, e) =>
            {
                connection.Dispose();
                Consume(queue);
            };

            channel.QueueDeclare(queue, true, false, false);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += delegate(Object chan, BasicDeliverEventArgs ea)
            {
                // Check logged in to Steam
                if (!Steam.steamClient.IsConnected || !Steam.isLoggedOn)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Log.GoogleInfo("Waiting to login before consuming");
                    channel.BasicNack(ea.DeliveryTag, false, true);
                    return;
                }

                // Consume message
                var response = HandleMessage(ea);
                var ack = response.Result.Item1;
                var requeue = response.Result.Item2;

                if (ack)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    channel.BasicNack(ea.DeliveryTag, false, requeue);
                }
            };

            channel.BasicConsume(queue, false, consumer);
        }

        public static void connect()
        {
            if (connection == null || !connection.IsOpen || channel == null || !channel.IsOpen)
            {
                connection = connectionFactory.CreateConnection();
                channel = connection.CreateModel();
            }
        }

        protected async void GetAccessTokens(IEnumerable<UInt32> apps, IEnumerable<UInt32> packages)
        {
            var JobID = Steam.steamApps.PICSGetAccessTokens(apps, packages);
            var callback = await JobID;

            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }
    }
}