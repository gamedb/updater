using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RabbitMQ.Client.Events;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Updater.Consumers
{
    public class AppConsumer : AbstractConsumer
    {
        protected override async Task HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            // Get the full payload
            BaseMessage payload;
            try
            {
                payload = JsonConvert.DeserializeObject<BaseMessage>(msgBody);
            }
            catch (JsonSerializationException e)
            {
                Log.GoogleInfo("Unable to deserialize app: " + e + " - " + e.InnerException + " - " + msgBody);
                return;
            }

            // Remove any keys that can't be deserialised
            var json = JObject.Parse(payload.Message.ToString());
            json.Property("PICSAppInfo").Remove();

            // Get the message in the payload
            AppMessage message;
            try
            {
                message = JsonConvert.DeserializeObject<AppMessage>(json.ToString());
            }
            catch (JsonSerializationException e)
            {
                Log.GoogleInfo("Unable to deserialize app message: " + e + " - " + e.InnerException + " - " + json);
                return;
            }

            var JobID = Steam.steamApps.PICSGetProductInfo(message.ID, null, false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                // Retry
                Produce(queue_cs_apps, payload, true);
                return;
            }

            foreach (var result in callback.Results)
            {
                // Send app
                foreach (var item in result.Apps)
                {
                    payload.Message = new AppMessage
                    {
                        ID = message.ID,
                        PICSAppInfo = item.Value
                    };
                    Produce(queue_go_apps, payload);
                }

                // Send unknown apps
                foreach (var entry in result.UnknownApps)
                {
                    Log.GoogleInfo("Unknown app: " + entry);

                    payload.Message = new AppMessage
                    {
                        ID = entry,
                        PICSAppInfo = null
                    };
                    Produce(queue_go_apps, payload);
                }
            }
        }
    }

    public class AppMessage
    {
        [JsonProperty(PropertyName = "id")]
        public UInt32 ID;

        public PICSProductInfo PICSAppInfo;

        public static BaseMessage create(UInt32 id, PICSProductInfo pics = null)
        {
            return new BaseMessage
            {
                Message = new AppMessage
                {
                    ID = id,
                    PICSAppInfo = pics
                }
            };
        }
    }
}