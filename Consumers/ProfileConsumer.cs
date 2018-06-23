using System.Text;
using RabbitMQ.Client.Events;
using SteamKit2;

namespace SteamUpdater.Consumers
{
    public class ProfileConsumer : AbstractConsumer
    {
        protected override void HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            var id = new SteamID();
            id.SetFromUInt64(ulong.Parse(msgBody));

            Steam.steamFriends.RequestProfileInfo(id);
        }
    }
}