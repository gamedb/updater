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
        protected override async Task<Boolean> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            PackageMessageIn payload;
            try
            {
                payload = JsonConvert.DeserializeObject<PackageMessageIn>(msgBody);
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine("Unable to deserialize package: " + msgBody);
                return false;
            }

            if (payload.IDs.Length == 0)
            {
                return false;
            }

            var packageIDs = Array.ConvertAll(payload.IDs, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(new List<UInt32>(), packageIDs, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return true;
            }

            foreach (var result in callback.Results)
            {
                foreach (var item in result.Packages)
                {
                    var message = new PackageMessageOut
                    {
                        PICSPackageInfo = item.Value,
                        Payload = payload
                    };

                    Produce(queuePackagesData, JsonConvert.SerializeObject(message));
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

            return false;
        }
    }

    public class PackageMessageIn : BaseMessage
    {
        public UInt32 ID { get; set; }¬
        public PICSProductInfo PICSPackageInfo { get; set; }
    }
}