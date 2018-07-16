using System;

namespace SteamUpdater
{
    public static class Config
    {
        public static string quitOnDisconnect;
        
        public static void setEnvVars()
        {
            // todo add validation if emtpy
            Environment.GetEnvironmentVariable("ENV");
            Environment.GetEnvironmentVariable("STEAM_GOOGLE_PROJECT");
            
            Environment.GetEnvironmentVariable("STEAM_PROXY_ROLLBAR_PRIVATE");
            Environment.GetEnvironmentVariable("STEAM_PROXY_USERNAME");
            Environment.GetEnvironmentVariable("STEAM_PROXY_PASSWORD");
            
            Environment.GetEnvironmentVariable("STEAM_RABBIT_HOST");
            Environment.GetEnvironmentVariable("STEAM_RABBIT_USER");
            Environment.GetEnvironmentVariable("STEAM_RABBIT_PASS");
        }
    }
}