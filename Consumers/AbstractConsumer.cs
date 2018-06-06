using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;
using System.Threading.Tasks;

namespace SteamProxy.Consumers
{
    public class AbstractConsumer
    {
        protected static IConnection connection;
        protected static IModel channel;

        protected string queueName;

        public AbstractConsumer(string name)
        {
            queueName = name;
        }

        public static void startConsumers()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                /* run your code here */
                Console.WriteLine("Hello, world");
            }).Start();
        }

        protected void getConnection()
        {
            if (connection == null || channel == null)
            {
                var factory = new ConnectionFactory {HostName = "localhost"};

                connection = factory.CreateConnection();
                channel = connection.CreateModel();
            }
        }
    }
}