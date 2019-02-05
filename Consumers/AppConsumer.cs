using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Updater.Consumers
{
    public class AppConsumer : AbstractConsumer
    {
        protected override async Task<Boolean> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            AppMessage payload;
            try
            {
                payload = JsonConvert.DeserializeObject<AppMessage>(msgBody);
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine("Unable to deserialize app: " + msgBody);
                return false;
            }

            if (payload.IDs.Length == 0)
            {
                return false;
            }

            var appIDs = Array.ConvertAll(payload.IDs, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(appIDs, new List<UInt32>(), false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return true;
            }

            foreach (var result in callback.Results)
            {
                foreach (var item in result.Apps)
                {
                    var message = new AppMessageOut
                    {
                        PICSAppInfo = item.Value,
                        Payload = payload
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

            return false;
        }
    }

    public class AppMessage : BaseMessage
    {
        public UInt32 ID { get; set; }
        public PICSProductInfo PICSAppInfo { get; set; }
    }
}