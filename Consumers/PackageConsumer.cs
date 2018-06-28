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
        protected override async Task<bool> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");

            if (ids.Length == 0)
            {
                return true;
            }

            var packageIDs = Array.ConvertAll(ids, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(new List<uint>(), packageIDs, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return false;
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
            }

            return true;
        }
    }
}