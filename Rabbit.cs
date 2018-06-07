using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Threading;

namespace SteamProxy
{
    public class Rabbit
    {
        public const string queueAppIds = "app-ids";
        public const string queueAppDatas = "app-datas";
        public const string queuePackageIds = "package-ids";
        public const string queuePackageDatas = "package-datas";

        private readonly string queueName;

        private Rabbit(string name)
        {
            queueName = "STEAM_PROXY_" + name;
        }

        public static void startConsumers()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var apps = new Rabbit("app-ids");
                apps.Consume();
            }).Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var packages = new Rabbit("package-ids");
                packages.Consume();
            }).Start();
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
            connection.AutoClose = false;
            var channel = connection.CreateModel();

            return (connection, channel);
        }

        public static void Produce(string queue, string data)
        {
            var x = getConnection();
            var connection = x.Item1;
            var channel = x.Item2;

            channel.QueueDeclare(queue, true, false, false, null);

            var bytes = Encoding.UTF8.GetBytes(data);

            channel.BasicPublish("", queue, null, bytes);

            Console.WriteLine("Sent to Rabbit: {0}", data);

            channel.Close();
            connection.Close();
        }

        private void Consume()
        {
            while (true)
            {
                Console.WriteLine("Connecting to Rabbit");
                var x = getConnection();
                var connection = x.Item1;
                var channel = x.Item2;

                connection.ConnectionShutdown += (s, e) => { connection.Dispose(); };

                channel.QueueDeclare(queueName, false, false, false, null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var message = Encoding.UTF8.GetString(ea.Body);
                    Console.WriteLine("Read Rabbit message: {0}", message);
                };

                channel.BasicConsume(queueName, true, consumer);

                break;
            }
        }
    }
}