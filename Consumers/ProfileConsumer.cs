using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SteamKit2;
using static SteamKit2.SteamFriends;

namespace Updater.Consumers
{
    public class ProfileConsumer : AbstractConsumer
    {
        protected override async Task HandleMessage(BaseMessage payload)
        {
            // Remove any keys that can't be deserialised
            var json = JObject.Parse(JsonConvert.SerializeObject(payload.Message));
            var key = json.Property("PICSProfileInfo");
            key?.Remove();

            // Get the message in the payload
            var message = JsonConvert.DeserializeObject<ProfileMessage>(json.ToString());

            // Get profile data
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

        // ReSharper disable once NotAccessedField.Global
        public ProfileInfoCallback PICSProfileInfo;
    }
}