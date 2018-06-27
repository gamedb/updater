using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public class PackageConsumer : AbstractConsumer
    {
        protected override void HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");

            if (ids.Length > 0)
            {
                // Un-bulk and re-produce
                foreach (var entry in ids)
                {
                    Produce(queuePackages, entry);
                }
            }
            else if (ids.Length == 1)
            {
                var job = Steam.steamApps.PICSGetProductInfo(null, Convert.ToUInt32(ids[0]), false, false);

                var callback = await job;

                foreach (var item in callback.Results)
                {
                    Produce(queuePackagesData, JsonConvert.SerializeObject(item.Packages.First()));
                }

                Console.WriteLine(JsonConvert.SerializeObject(callback));
            }

            return true;
        }
    }
}