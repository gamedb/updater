﻿using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace SteamUpdater.Consumers
{
    public class AppConsumer : AbstractConsumer
    {
        protected override async Task<bool> HandleMessage(BasicDeliverEventArgs msg)
        {
            var msgBody = Encoding.UTF8.GetString(msg.Body);
            var ids = msgBody.Split(",");

            if (ids.Length > 0)
            {
                uint[] empty = { };
                var idInts = Array.ConvertAll(ids, Convert.ToUInt32);
                Steam.steamApps.PICSGetProductInfo(idInts, empty, false);
            }

            return true;
        }
    }
}