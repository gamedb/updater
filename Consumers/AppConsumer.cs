using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using Newtonsoft.Json;
using SteamUpdater.Consumers.Messages;

namespace SteamUpdater.Consumers
{
    public class AppConsumer : AbstractConsumer
    {
        protected override async Task<bool> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");

            if (ids.Length == 0)
            {
                return true;
            }

            var appIDs = Array.ConvertAll(ids, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(appIDs, new List<uint>(), false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return false;
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
            }

            return true;
        }

        protected void GetGlobalAchievementPercentagesForApp()
        {
            
        }

        protected void GetSchemaForGame()
        {
            
        }
    }
}