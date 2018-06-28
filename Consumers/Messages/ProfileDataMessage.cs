using SteamKit2;

namespace SteamUpdater.Consumers.Messages
{
    public class ProfileDataMessage
    {
        public SteamFriends.ProfileInfoCallback ProfileInfo { get; set; }
    }
}