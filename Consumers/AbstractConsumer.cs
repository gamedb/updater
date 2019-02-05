﻿using System;
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
            UserName = Environment.GetEnvironmentVariable("STEAM_RABBIT_USER"),
            Password = Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS"),
            HostName = Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST"),
            Port = Int32.Parse(Environment.GetEnvironmentVariable("STEAM_RABBIT_PORT")),
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

        public static void Produce(String queue, BaseMessage payload)
        {
            if (payload.Attempt == 0)
            {
                payload.Attempt = 1;
            }

            if (payload.FirstSeen == DateTime.MinValue)
            {
                payload.FirstSeen = DateTime.Now;
            }

            if (payload.OriginalQueue == "")
            {
                // If Go errors, we want to put it back into the go queue
                //payload.OriginalQueue = queue;
            }


            var payloadString = JsonConvert.SerializeObject(payload);

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
                HandleMessage(msg);

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

        protected async void GetAccessTokens(IEnumerable<UInt32> apps, IEnumerable<UInt32> packages)
        {
            var JobID = Steam.steamApps.PICSGetAccessTokens(apps, packages);
            var callback = await JobID;

            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }
    }

    public class BaseMessage
    {
        public Object Message { get; set; }
        public DateTime FirstSeen { get; set; }
        public Int32 Attempt { get; set; }
        public String OriginalQueue { get; set; }
        public Int32 MaxAttempts { get; set; }
        public Int32 MaxTime { get; set; }
    }
}