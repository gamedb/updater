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
        protected override async Task HandleMessage(BasicDeliverEventArgs msg)
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
                return;
            }

            var JobID = Steam.steamApps.PICSGetProductInfo(payload.ID, null, false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                // Retry
                Produce(queue_cs_apps, payload);
                return;
            }

            foreach (var result in callback.Results)
            {
                foreach (var item in result.Apps)
                {
                    payload.PICSAppInfo = item.Value;

                    // Send to Go
                    Produce(queue_go_apps, payload);
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

    public abstract class AppMessage : BaseMessage
    {
        public UInt32 ID { get; set; }
        public PICSProductInfo PICSAppInfo { get; set; }
    }
}