using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SteamUpdater.Consumers.Messages;

namespace SteamUpdater.Consumers
{
    public class AppConsumer : AbstractConsumer
    {
        protected override async Task<Tuple<bool, bool>> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var IDs = msgBody.Split(",").Distinct().ToArray();

            if (msgBody.Length == 0 || IDs.Length == 0)
            {
                return new Tuple<bool, bool>(false, false);
            }

            var appIDs = Array.ConvertAll(IDs, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(appIDs, new List<uint>(), false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return new Tuple<bool, bool>(false, true);
            }

            foreach (var result in callback.Results)
            {
                foreach (var item in result.Apps)
                {
                    var message = new AppDataMessage
                    {
                        PICSAppInfo = item.Value
                    };

                    Produce(queueAppsData, JsonConvert.SerializeObject(message));
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