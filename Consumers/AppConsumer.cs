using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Updater.Consumers
{
    public class AppConsumer : AbstractConsumer
    {
        protected override async Task HandleMessage(BaseMessage payload)
        {
            // Remove any keys that can't be deserialised
            var json = JObject.Parse(JsonConvert.SerializeObject(payload.Message));
            var key = json.Property("PICSAppInfo");
            key?.Remove();

            // Get the message in the payload
            var message = JsonConvert.DeserializeObject<AppMessage>(json.ToString());

            var JobID = Steam.steamApps.PICSGetProductInfo(message.ID, null, false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                throw new Exception("Callback not complete");
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

        // ReSharper disable once NotAccessedField.Global
        public PICSProductInfo PICSAppInfo;

        public static BaseMessage create(UInt32 id)
        {
            return new BaseMessage
            {
                Message = new AppMessage
                {
                    ID = id
                }
            };
        }
    }
}