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
        protected override async Task<Tuple<Boolean, Boolean>> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            
            AppMessageIn payload;
            try
            {
                payload = JsonConvert.DeserializeObject<AppMessageIn>(msgBody);
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine("Unable to deserialize app: " + msgBody);
                return new Tuple<Boolean, Boolean>(true, false);
            }
            
            if (payload.IDs.Length == 0)
            {
                return new Tuple<Boolean, Boolean>(false, false);
            }

            var appIDs = Array.ConvertAll(payload.IDs, Convert.ToUInt32);
            var JobID = Steam.steamApps.PICSGetProductInfo(appIDs, new List<UInt32>(), false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                return new Tuple<Boolean, Boolean>(false, true);
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

            return new Tuple<Boolean, Boolean>(true, false);
        }
    }

    public class AppMessageIn
    {
        public UInt32[] IDs { get; set; }
        public UInt64 Time { get; set; }
    }

    public class AppMessageOut
    {
        public PICSProductInfo PICSAppInfo { get; set; }
        public AppMessageIn Payload { get; set; }
    }
}