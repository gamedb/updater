using SteamKit2;

namespace SteamUpdater.Consumers.Messages
{
    public class ChangeDataMessage : AbstractMessage
    {
        public SteamApps.PICSChangesCallback PICSChanges { get; set; }
    }
}