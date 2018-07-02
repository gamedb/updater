using static SteamKit2.SteamApps;

namespace SteamUpdater.Consumers.Messages
{
    public class PackageDataMessage : AbstractMessage
    {
        public PICSProductInfoCallback.PICSProductInfo PICSPackageInfo { get; set; }
    }
}