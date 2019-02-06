using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using SteamKit2;
using static SteamKit2.SteamFriends;

namespace Updater.Consumers
{
    public class ProfileConsumer : AbstractConsumer
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
                Log.GoogleInfo("Unable to deserialize profile: " + msgBody);
                return;
            }

            // Get the message in the payload
            ProfileMessage message;
            try
            {
                message = JsonConvert.DeserializeObject<ProfileMessage>(payload.Message.ToString());
            }
            catch (Exception)
            {
                Log.GoogleInfo("Unable to deserialize app message: " + payload.Message);
                return;
            }

            var id = new SteamID();
            id.SetFromUInt64(message.ID);
            var JobID = Steam.steamFriends.RequestProfileInfo(id);
            var callback = await JobID;

            payload.Message = new ProfileMessage
            {
                ID = message.ID,
                PICSProfileInfo = callback
            };

            Produce(queue_go_profiles, payload);
        }
    }

    public class ProfileMessage
    {
        [JsonProperty(PropertyName = "id")]
        public UInt64 ID;

        public ProfileInfoCallback PICSProfileInfo;
    }
}