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
        public const String queue_go_changes = "GameDB_Go_Changes";
        public const String queue_go_delays = "GameDB_Go_Delays";
        public const String queue_go_packages = "GameDB_Go_Packages";
        public const String queue_go_profiles = "GameDB_Go_Profiles";

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
        protected abstract Task HandleMessage(BaseMessage payload);

        // Statics
        public static void startConsumers()
        {
            Log.Info("Loading consumers");

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

        public static void Produce(String queue, BaseMessage payload)
        {
            if (queue == "")
            {
                payload.OriginalQueue = queue;
            }

            if (payload.FirstSeen == DateTime.MinValue)
            {
                payload.FirstSeen = DateTime.Now;
            }

            if (payload.Attempt == 0)
            {
                payload.Attempt = 1;
            }

            var formatting = Config.isLocal() ? Formatting.Indented : Formatting.None;
            var payloadString = JsonConvert.SerializeObject(payload, formatting);

            try
            {
                waitForConnections();

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
                Log.Error("Failed producing to " + queue + " with data: " + payloadString + " - " + ex.Message);
            }
        }

        private void Consume(String queue)
        {
            waitForConnections();

            var connection = getConsumerConnection();
            var channel = connection.CreateModel();

            channel.BasicQos(0, 10, false);
            channel.QueueDeclare(queue, true, false, false);

            var consumer = new EventingBasicConsumer(channel);
            var payload = new BaseMessage();

            consumer.Received += delegate(Object chan, BasicDeliverEventArgs msg)
            {
                var msgBody = Encoding.UTF8.GetString(msg.Body);

                // Make a message object
                try
                {
                    payload = JsonConvert.DeserializeObject<BaseMessage>(msgBody);
                }
                catch (Exception e)
                {
                    Log.Error("Unable to deserialize message body: " + e + " - " + e.InnerException + " - " + msgBody);
                    payload.ack(channel, msg);
                    return;
                }

                // Check logged in to Steam
                if (!Steam.steamClient.IsConnected || !Steam.isLoggedOn)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    Log.Info("Waiting to login before consuming");
                    payload.ackRetry(channel, msg);
                    return;
                }

                // Consume message
                var task = HandleMessage(payload);
                if (task.Exception is AggregateException)
                {
                    Log.Info(task.Exception + " - " + task.Exception.InnerException);
                    payload.ack(channel, msg);
                    return;
                }

                if (task.Exception is JsonSerializationException)
                {
                    Log.Error(task.Exception + " - " + task.Exception.InnerException);
                    payload.ack(channel, msg);
                    return;
                }

                if (task.Exception != null)
                {
                    Log.Error(task.Exception + " - " + task.Exception.InnerException);
                    payload.ackRetry(channel, msg);
                    return;
                }

                payload.ack(channel, msg);
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
                    Log.Error("Producer connection lost");

                    producerConnection.Dispose();
                    producerConnection = null;

                    waitForConnections();
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
                    Log.Error("Consumer connection lost");

                    consumerConnection.Dispose();
                    consumerConnection = null;

                    waitForConnections();
                };
            }

            return consumerConnection;
        }

        public static void waitForConnections()
        {
            var count = 0;

            while (true)
            {
                count++;

                try
                {
                    getProducerConnection();
                    getConsumerConnection();
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error("Waiting for Rabbit (" + count + ") " + ex.Message + " - " + ex.InnerException.Message);
                }

                Thread.Sleep(TimeSpan.FromSeconds(5));
            }
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

        public void ack(IModel channel, BasicDeliverEventArgs msg)
        {
            channel.BasicAck(msg.DeliveryTag, false);
        }

        public void ackRetry(IModel channel, BasicDeliverEventArgs msg)
        {
            Attempt++;
            Log.Info("Adding to delay queue");

            AbstractConsumer.Produce(AbstractConsumer.queue_go_delays, this);

            channel.BasicAck(msg.DeliveryTag, false);
        }
    }
}