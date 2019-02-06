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

        public const String queue_cs_apps = "GameDB_CS_Apps";
        public const String queue_cs_packages = "GameDB_CS_Packages";
        public const String queue_cs_profiles = "GameDB_CS_Profiles";

        public const String queue_go_apps = "GameDB_Go_Apps";
        public const String queue_go_packages = "GameDB_Go_Packages";
        public const String queue_go_profiles = "GameDB_Go_Profiles";
        public const String queue_go_changes = "GameDB_Go_Changes";

        // Queue -> Consumer
        public static readonly Dictionary<String, AbstractConsumer> consumers = new Dictionary<String, AbstractConsumer>
        {
            {queue_cs_apps, new AppConsumer()},
            {queue_cs_packages, new PackageConsumer()},
            {queue_cs_profiles, new ProfileConsumer()}
        };

        //
        private static readonly ConnectionFactory connectionFactory = new ConnectionFactory
        {
            UserName = Config.rabbitUsername,
            Password = Config.rabbitPassword,
            HostName = Config.rabbitHostname,
            Port = Config.rabbitPort,
            RequestedHeartbeat = 10
        };

        // Abstract
        protected abstract Task HandleMessage(BasicDeliverEventArgs msg);

        // Statics
        public static void startConsumers()
        {
            foreach (var (key, consumer) in consumers)
            {
                var thread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;

                    consumer.Consume(key);
                });

                thread.Start();
            }
        }

        public static void Produce(String queue, BaseMessage payload, Boolean retry = false)
        {
            if (payload.Attempt == 0)
            {
                payload.Attempt = 1;
            }

            if (retry)
            {
                payload.Attempt++;
            }

            if (payload.FirstSeen == DateTime.MinValue)
            {
                payload.FirstSeen = DateTime.Now;
            }

            if (String.IsNullOrEmpty(payload.OriginalQueue))
            {
                payload.OriginalQueue = queue;
            }

            var formatting = Config.isLocal() ? Formatting.Indented : Formatting.None;
            var payloadString = JsonConvert.SerializeObject(payload, formatting);

            try
            {
                var connection = getProducerConnection();
                var channel = connection.CreateModel();

                channel.QueueDeclare(queue, true, false, false);

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                var bytes = Encoding.UTF8.GetBytes(payloadString);
                channel.BasicPublish("", queue, properties, bytes);

                channel.Close();
            }
            catch (Exception ex)
            {
                Log.RollbarError("Failed producing to " + queue + " with data: " + payloadString + " - " + ex.Message);
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
            consumer.Received += delegate(Object chan, BasicDeliverEventArgs msg)
            {
                // Check logged in to Steam
                if (!Steam.steamClient.IsConnected || !Steam.isLoggedOn)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Log.GoogleInfo("Waiting to login before consuming");
                    channel.BasicNack(msg.DeliveryTag, false, true);
                    return;
                }

                // Consume message
                try
                {
                    HandleMessage(msg);
                }
                catch (Exception e)
                {
                    Log.GoogleInfo(e + " - " + e.InnerException);
                    return;
                }

                channel.BasicAck(msg.DeliveryTag, false);
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
    }

    public class BaseMessage
    {
        [JsonProperty(PropertyName = "message")]
        public Object Message;

        [JsonProperty(PropertyName = "first_seen")]
        public DateTime FirstSeen;

        [JsonProperty(PropertyName = "attempt")]
        public Int32 Attempt;

        [JsonProperty(PropertyName = "original_queue")]
        public String OriginalQueue;

        [JsonProperty(PropertyName = "max_attempts")]
        public Int32 MaxAttempts;

        [JsonProperty(PropertyName = "max_time")]
        public Int32 MaxTime;
    }
}