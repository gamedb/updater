using SteamKit2;

namespace Updater.Consumers.Messages
{
    public class ProfileDataMessage : AbstractMessage
    {
        public SteamFriends.ProfileInfoCallback ProfileInfo { get; set; }
    }
}