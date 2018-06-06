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

        private static IConnection connection;
        private static IModel channel;
        private string queueName;

        public Rabbit(string name)
        {
            queueName = name;
        }

        public static void startConsumers()
        {
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var apps = new Rabbit("app-ids");
                apps
            }).Start();

            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var packages = new Rabbit("package-ids");
            }).Start();
        }

        protected void getConnection()
        {
            if (connection == null || channel == null)
            {
                var factory = new ConnectionFactory {HostName = "localhost"};

                connection = factory.CreateConnection();
                channel = connection.CreateModel();
                
                // connection.ConnectionShutdown += // todo
            }
        }

        public static void Produce(string queue, string data)
        {
            var factory = new ConnectionFactory {HostName = "localhost"};

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue, true, false, false, null);

                var bytes = Encoding.UTF8.GetBytes(data);

                channel.BasicPublish("", queue, null, bytes);

                Console.WriteLine("[x] Sent to Rabbit");
            }
        }

        public static void Consume()
        {
            channel.QueueDeclare("hello", false, false, false, null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body;
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" [x] Received {0}", message);
            };
            channel.BasicConsume("hello", true, consumer);

            Console.WriteLine(" Press [enter] to exit.");
            Console.ReadLine();
        }
    }
}