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
                Console.WriteLine("Unable to deserialize profile: " + msgBody);
                return;
            }

            var id = new SteamID();
            id.SetFromUInt64(payload.ID);
            var JobID = Steam.steamFriends.RequestProfileInfo(id);

            payload.PICSProfileInfo = await JobID;

            Produce(queue_go_profiles, payload);
        }
    }

    public abstract class ProfileMessage : BaseMessage
    {
        public UInt64 ID { get; set; }
        public ProfileInfoCallback PICSProfileInfo { get; set; }
    }
}