using SteamKit2;

namespace SteamUpdater.Consumers.Messages
{
    public class ProfileDataMessage : AbstractMessage
    {
        public SteamFriends.ProfileInfoCallback ProfileInfo { get; set; }
    }
}