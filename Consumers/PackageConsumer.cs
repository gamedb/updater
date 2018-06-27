using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public class PackageConsumer : AbstractConsumer
    {
        protected override async Task<bool> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");

            if (ids.Length > 1)
            {
                // Un-bulk and re-produce
                foreach (var entry in ids)
                {
                    Produce(queuePackages, entry);
                }
            }
            else if (ids.Length == 1)
            {
                var job = Steam.steamApps.PICSGetProductInfo(null, Convert.ToUInt32(ids[0]), false, false);

                var callback = await job;

                foreach (var item in callback.Results)
                {
                    Produce(queuePackagesData, JsonConvert.SerializeObject(item.Packages.First()));
                }

                Console.WriteLine(JsonConvert.SerializeObject(callback));
            }

            return true;
        }

        protected void getPackageDetails()
        {
            // this will map to the ISteamUser endpoint
            var steamInterface = new SteamUser("<devKeyHere>");

// this will map to ISteamUser/GetPlayerSummaries method in the Steam Web API
// see PlayerSummaryResultContainer.cs for response documentation
            var playerSummaryResponse = await steamInterface.GetPlayerSummaryAsync( < steamIdHere >);
            var playerSummaryData = playerSummaryResponse.Data;
            var playerSummaryLastModified = playerSummaryResponse.LastModified;

// this will map to ISteamUser/GetFriendsListAsync method in the Steam Web API
// see FriendListResultContainer.cs for response documentation
            var friendsListResponse = await steamInterface.GetFriendsListAsync( < steamIdHere >);
            var friendsList = friendsListResponse.Data;
        }
    }
}