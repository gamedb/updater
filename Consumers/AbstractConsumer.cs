using System;
using RabbitMQ.Client;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public abstract class AbstractConsumer
    {
        // Consts
        public const string queueApps = "Apps";
        protected const string queueAppsData = "Apps_Data";

        public const string queuePackages = "Packages";
        protected const string queuePackagesData = "Packages_Data";

        public const string queueProfiles = "Profiles";
        protected const string queueProfilesData = "Profiles_Data";

        public const string queueChangesData = "Changes_Data";

        private const string queueAppend = "Steam_";

        // Queue -> Consumer
        public static readonly Dictionary<string, AbstractConsumer> consumers = new Dictionary<string, AbstractConsumer>
        {
            {queueApps, new AppConsumer()},
            {queuePackages, new PackageConsumer()},
            {queueProfiles, new ProfileConsumer()}
        };

        //
        private static readonly ConnectionFactory connectionFactory = new ConnectionFactory
        {
            HostName = Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST"),
            UserName = Environment.GetEnvironmentVariable("STEAM_RABBIT_USER"),
            Password = Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS")
        };

        // Abstract
        protected abstract Task<Tuple<bool, bool>> HandleMessage(BasicDeliverEventArgs msg);

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

        public static void Produce(string queue, string data)
        {
            if (data.Length == 0)
            {
                return;
            }

            try
            {
                // Get connection
                var x = getConnection();
                var connection = x.Item1;
                var channel = x.Item2;

                //
                channel.QueueDeclare(queueAppend + queue, true, false, false);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                var bytes = Encoding.UTF8.GetBytes(data);
                channel.BasicPublish("", queueAppend + queue, properties, bytes);

                // Close connection
                if (!channel.IsClosed)
                {
                    channel.Close();
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                Log.RollbarError("Failed producing to " + queue + " with data: " + data + " - " + ex.Message);
            }
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

        private static (IConnection, IModel) getConnection()
        {
            var connection = connectionFactory.CreateConnection();
            var channel = connection.CreateModel();

            return (connection, channel);
        }

        protected async void GetAccessTokens(IEnumerable<uint> apps, IEnumerable<uint> packages)
        {
            var JobID = Steam.steamApps.PICSGetAccessTokens(apps, packages);
            var callback = await JobID;

            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }
    }
}