using SteamKit2;

namespace SteamUpdater.Consumers.Messages
{
    public class ChangeDataMessage
    {
        public SteamApps.PICSChangesCallback PICSChanges { get; set; }
    }
}