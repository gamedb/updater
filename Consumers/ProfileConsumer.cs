using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using SteamKit2;
using SteamUpdater.Consumers.Messages;

namespace SteamUpdater.Consumers
{
    public class ProfileConsumer : AbstractConsumer
    {
        protected override async Task<Tuple<bool, bool>> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            if (msgBody.Length == 0)
            {
                return new Tuple<bool, bool>(false, false);
            }

            var id = new SteamID();
            id.SetFromUInt64(ulong.Parse(msgBody));

            var JobID = Steam.steamFriends.RequestProfileInfo(id);

            var message = new ProfileDataMessage
            {
                ProfileInfo = await JobID
            };

            Produce(queueProfilesData, JsonConvert.SerializeObject(message));

            return new Tuple<bool, bool>(true, false);
        }
    }
}