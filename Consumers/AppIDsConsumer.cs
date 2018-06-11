using System;
using System.Text;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public class AppIDsConsumer : AbstractConsumer
    {
        protected override void HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");

            if (ids.Length > 0)
            {
                var idInts = Array.ConvertAll(ids, Convert.ToUInt32);
                Steam.steamApps.PICSGetProductInfo(idInts, null, false);
            }
        }
    }
}