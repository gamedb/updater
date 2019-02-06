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

            ProfileMessage payload;
            try
            {
                payload = JsonConvert.DeserializeObject<ProfileMessage>(msgBody);
            }
            catch (JsonSerializationException)
            {
                Log.GoogleInfo("Unable to deserialize profile: " + msgBody);
                return;
            }

            var id = new SteamID();
            id.SetFromUInt64(payload.ID);
            var JobID = Steam.steamFriends.RequestProfileInfo(id);
            var callback = await JobID;

            payload.PICSProfileInfo = callback;

            Produce(queue_go_profiles, payload);
        }
    }

    public abstract class ProfileMessage
    {
        [JsonProperty(PropertyName = "id")]
        public UInt64 ID;
        public ProfileInfoCallback PICSProfileInfo;
    }
}