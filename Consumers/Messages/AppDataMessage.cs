using SteamKit2;

namespace Updater.Consumers.Messages
{
    public class AppDataMessage : AbstractMessage
    {
        public SteamApps.PICSProductInfoCallback.PICSProductInfo PICSAppInfo { get; set; }
    }
}