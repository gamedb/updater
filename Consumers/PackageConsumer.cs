using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using static SteamKit2.SteamApps.PICSProductInfoCallback;

namespace Updater.Consumers
{
    public class PackageConsumer : AbstractConsumer
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
            catch (JsonSerializationException)
            {
                Log.GoogleInfo("Unable to deserialize package: " + msgBody);
                return;
            }

            // Get the message in the payload
            PackageMessage message;
            try
            {
                message = JsonConvert.DeserializeObject<PackageMessage>(payload.Message.ToString());
            }
            catch (Exception)
            {
                Log.GoogleInfo("Unable to deserialize app message: " + payload.Message);
                return;
            }

            var JobID = Steam.steamApps.PICSGetProductInfo(null, message.ID, false, false);
            var callback = await JobID;

            if (!callback.Complete)
            {
                // Retry
                Produce(queue_cs_packages, payload, true);
                return;
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
                    Log.GoogleInfo("Unknown package: " + entry);

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

        public PICSProductInfo PICSPackageInfo;

        public static BaseMessage create(UInt32 id, PICSProductInfo pics = null)
        {
            return new BaseMessage
            {
                Message = new PackageMessage
                {
                    ID = id,
                    PICSPackageInfo = pics
                }
            };
        }
    }
}