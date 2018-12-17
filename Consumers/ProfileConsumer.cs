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
        protected override async Task<Boolean> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            ProfileMessageIn payload;
            try
            {
                payload = JsonConvert.DeserializeObject<ProfileMessageIn>(msgBody);
            }
            catch (JsonSerializationException)
            {
                Console.WriteLine("Unable to deserialize profile: " + msgBody);
                return false;
            }

            if (payload.ID == 0)
            {
                return false;
            }

            var id = new SteamID();
            id.SetFromUInt64(UInt64.Parse(msgBody));

            var JobID = Steam.steamFriends.RequestProfileInfo(id);

            var message = new ProfileMessageOut
            {
                ProfileInfo = await JobID,
                Payload = payload
            };

            Produce(queueProfilesData, JsonConvert.SerializeObject(message));

            return false;
        }
    }

    public class ProfileMessageIn
    {
        public UInt64 ID { get; set; }
        public UInt64 Time { get; set; }
    }

    public class ProfileMessageOut
    {
        public ProfileInfoCallback ProfileInfo { get; set; }
        public ProfileMessageIn Payload { get; set; }
    }
}