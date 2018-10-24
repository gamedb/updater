using SteamKit2;

namespace Updater.Consumers.Messages
{
    public class PackageDataMessage : AbstractMessage
    {
        public SteamApps.PICSProductInfoCallback.PICSProductInfo PICSPackageInfo { get; set; }
    }
}