using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Updater.Consumers
{
    public class PackageConsumer : AbstractConsumer
    {
        protected override async Task HandleMessage(BaseMessage payload)
        {
            // Remove any keys that can't be deserialised
            var json = JObject.Parse(JsonConvert.SerializeObject(payload));
            var key = json.Property("PICSPackageInfo");
            key?.Remove();

            // Get the message in the payload
            var message = JsonConvert.DeserializeObject<PackageMessage>(json.ToString());

            var JobID = Steam.steamApps.PICSGetProductInfo(null, message.ID, false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                throw new Exception("Callback not complete");
            }

            foreach (var result in callback.Results)
            {
                // Send package
                foreach (var item in result.Packages)
                {
                    payload.Message = new AppMessage
                    {
                        ID = message.ID,
                        PICSAppInfo = item.Value
                    };
                    Produce(queue_go_packages, payload);
                }

                // Send unknown packages
                foreach (var entry in result.UnknownPackages)
                {
                    payload.Message = new AppMessage
                    {
                        ID = entry,
                        PICSAppInfo = null
                    };
                    Produce(queue_go_packages, payload);
                }
            }
        }
    }

    public class PackageMessage
    {
        [JsonProperty(PropertyName = "id")]
        public UInt32 ID;

        
        // ReSharper disable once UnusedMember.Global
        public PICSProductInfo PICSPackageInfo;

        public static BaseMessage create(UInt32 id)
        {
            return new BaseMessage
            {
                Message = new PackageMessage
                {
                    ID = id
                }
            };
        }
    }
}