using System;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;
using SteamKit2;

namespace SteamUpdater.Consumers
{
    public class ProfileConsumer : AbstractConsumer
    {
        protected override async void HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);

            var id = new SteamID();
            id.SetFromString(msgBody, EUniverse.Public);

            Console.WriteLine(id.ToString());

            var job = Steam.steamFriends.RequestProfileInfo(id);

            try
            {
                var res = await job;
                
                Console.WriteLine(JsonConvert.SerializeObject(res));
            }
            catch (Exception e)
            {
                Console.WriteLine(JsonConvert.SerializeObject(e));
            }

        }
    }
}