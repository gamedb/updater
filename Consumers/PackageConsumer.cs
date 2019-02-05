using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Updater.Consumers
{
    public class PackageConsumer : AbstractConsumer
    {
        protected override async Task HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            PackageMessage payload;
            try
            {
                payload = JsonConvert.DeserializeObject<PackageMessage>(msgBody);
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine("Unable to deserialize package: " + msgBody);
                return;
            }

            var JobID = Steam.steamApps.PICSGetProductInfo(null, payload.ID, false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                // Retry
                Produce(queue_cs_packages, payload);
                return;
            }

            foreach (var result in callback.Results)
            {
                foreach (var item in result.Packages)
                {
                    payload.PICSPackageInfo = item.Value;

                    // Send to Go
                    Produce(queue_go_packages, payload);
                }

                // Log unknowns
                foreach (var entry in result.UnknownApps)
                {
                    Log.GoogleInfo("Unknown app: " + entry);
                }

                foreach (var entry in result.UnknownPackages)
                {
                    Log.GoogleInfo("Unknown package: " + entry);
                }
            }
        }
    }

    public abstract class PackageMessage : BaseMessage
    {
        public UInt32 ID { get; set; }
        public PICSProductInfo PICSPackageInfo { get; set; }
    }
}