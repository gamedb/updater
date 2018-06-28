using SteamKit2;

namespace SteamUpdater.Consumers.Messages
{
    public class AppDataMessage
    {
        public SteamApps.PICSProductInfoCallback.PICSProductInfo PICSAppInfo { get; set; }
        public string GlobalAchievementPercentages { get; set; }
        public string GetSchemaForGame { get; set; }
    }
}