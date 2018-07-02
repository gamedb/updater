using SteamKit2;

namespace SteamUpdater.Consumers.Messages
{
    public class AppDataMessage : AbstractMessage
    {
        public SteamApps.PICSProductInfoCallback.PICSProductInfo PICSAppInfo { get; set; }
    }
}