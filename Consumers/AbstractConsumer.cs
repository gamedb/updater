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
        private static IConnection producerConnection;
        private static IConnection consumerConnection;

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
            Port = Int32.Parse(Environment.GetEnvironmentVariable("STEAM_RABBIT_PORT")),
            RequestedHeartbeat = 10
        };

        // Abstract
        protected abstract Task<Boolean> HandleMessage(BasicDeliverEventArgs msg);

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
                var connection = getProducerConnection();
                var channel = connection.CreateModel();

                channel.QueueDeclare(queueAppend + queue, true, false, false);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                var bytes = Encoding.UTF8.GetBytes(data);
                channel.BasicPublish("", queueAppend + queue, properties, bytes);

                channel.Close();
            }
            catch (Exception ex)
            {
                Log.RollbarError("Failed producing to " + queue + " with data: " + data + " - " + ex.Message);
            }
        }

        private void Consume(String queue)
        {
            Log.GoogleInfo("Consuming " + queue);

            var connection = getConsumerConnection();
            var channel = connection.CreateModel();
            channel.BasicQos(0, 10, false);

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
                var requeue = HandleMessage(ea);

                if (requeue.Result)
                {
                    Produce(queue, JsonConvert.SerializeObject(ea.Body));
                }

                channel.BasicAck(ea.DeliveryTag, false);
            };

            channel.BasicConsume(queue, false, consumer);
        }

        public static IConnection getProducerConnection()
        {
            if (producerConnection == null || !producerConnection.IsOpen)
            {
                producerConnection = connectionFactory.CreateConnection();

                producerConnection.ConnectionShutdown += (s, e) =>
                {
                    producerConnection.Dispose();
                    producerConnection = null;
                    throw new Exception("Producer connection lost");
                };
            }

            return producerConnection;
        }

        public static IConnection getConsumerConnection()
        {
            if (consumerConnection == null || !consumerConnection.IsOpen)
            {
                consumerConnection = connectionFactory.CreateConnection();

                consumerConnection.ConnectionShutdown += (s, e) =>
                {
                    consumerConnection.Dispose();
                    consumerConnection = null;
                };
            }

            return consumerConnection;
        }

        protected async void GetAccessTokens(IEnumerable<UInt32> apps, IEnumerable<UInt32> packages)
        {
            var JobID = Steam.steamApps.PICSGetAccessTokens(apps, packages);
            var callback = await JobID;

            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }
    }
}