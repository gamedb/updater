using SteamKit2;

namespace Updater.Consumers.Messages
{
    public class ChangeDataMessage : AbstractMessage
    {
        public SteamApps.PICSChangesCallback PICSChanges { get; set; }
    }
}