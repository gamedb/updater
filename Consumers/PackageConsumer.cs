using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using SteamUpdater.Consumers.Messages;

namespace SteamUpdater.Consumers
{
    public class PackageConsumer : AbstractConsumer
    {
        protected override async Task<Tuple<bool, bool>> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var IDs = msgBody.Split(",");

            if (msgBody.Length == 0 || IDs.Length == 0)
            {
                return new Tuple<bool, bool>(false, false);
            }

            // todo, take 100, requeue rest

            var packageIDs = Array.ConvertAll(IDs, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(new List<uint>(), packageIDs, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return new Tuple<bool, bool>(false, true);
            }

            foreach (var result in callback.Results)
            {
                foreach (var item in result.Packages)
                {
                    var message = new PackageDataMessage
                    {
                        PICSPackageInfo = item.Value
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

            return new Tuple<bool, bool>(true, false);
        }
    }
}