using System;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamUpdater
{
    public static class Steam
    {
        private const string LastChangeFile = "last-changenumber.txt";

        public static bool quitOnDisconnect;
        private static uint previousChangeNumber;
        private static bool isLoggedOn;

        private static SteamClient steamClient;
        private static CallbackManager manager;

        private static System.Timers.Timer timer1;
        private static System.Timers.Timer timer2;

        public static SteamUser steamUser;
        public static SteamApps steamApps;
        public static SteamFriends steamFriends;

        public static void startSteam(bool debug)
        {
            // Debug
            DebugLog.AddListener(new DebugListener());
            DebugLog.Enabled = debug;

            // Setup client
            steamClient = new SteamClient();
            manager = new CallbackManager(steamClient);

            steamUser = steamClient.GetHandler<SteamUser>();
            steamApps = steamClient.GetHandler<SteamApps>();
            steamFriends = steamClient.GetHandler<SteamFriends>();

            // Callbacks
            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            manager.Subscribe<SteamApps.PICSChangesCallback>(OnPicsChanges);

            steamClient.Connect();

            timer1 = new System.Timers.Timer();
            timer1.Elapsed += RunWaitCallbacks;
            timer1.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            timer1.Start();

            timer2 = new System.Timers.Timer();
            timer2.Elapsed += CheckForChanges;
            timer2.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            timer2.Start();
        }

        private static void RunWaitCallbacks(object obj, EventArgs args)
        {
            timer1.Stop();
            Console.WriteLine("1");
            manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            timer1.Start();
        }

        private static void CheckForChanges(object obj, EventArgs args)
        {
            timer2.Stop();
            
            // Get last change ID
            if (!steamClient.IsConnected || !isLoggedOn)
            {
                return;
            }

            if (previousChangeNumber == 0 && File.Exists(LastChangeFile))
            {
                previousChangeNumber = uint.Parse(File.ReadAllText(LastChangeFile));
            }

            // Check for new changes, if more than 5000, returns 0
            Console.WriteLine("2");
            steamApps.PICSGetChangesSince(previousChangeNumber, true, true);
            timer2.Start();
        }

        private static void OnPicsChanges(SteamApps.PICSChangesCallback callback)
        {
            if (previousChangeNumber == callback.CurrentChangeNumber)
            {
                return;
            }

            Log.GoogleInfo(
                String.Format(
                    "Change {0:N0} - {1:N0} ({2:N0} changes) {3} apps, {4} packages",
                    callback.LastChangeNumber,
                    callback.CurrentChangeNumber,
                    callback.CurrentChangeNumber - callback.LastChangeNumber,
                    callback.AppChanges.Count,
                    callback.PackageChanges.Count
                )
            );

            Consumers.AbstractConsumer.Produce(
                Consumers.AbstractConsumer.queueApps,
                string.Join(",", callback.AppChanges.Keys.ToList())
            );

            Consumers.AbstractConsumer.Produce(
                Consumers.AbstractConsumer.queuePackages,
                string.Join(",", callback.PackageChanges.Keys.ToList())
            );

            previousChangeNumber = callback.CurrentChangeNumber;
            File.WriteAllText(LastChangeFile, previousChangeNumber.ToString());

            Consumers.AbstractConsumer.Produce(
                Consumers.AbstractConsumer.queueChangesData,
                JsonConvert.SerializeObject(callback)
            );
        }

        private static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Log.GoogleInfo("Connected to Steam");
            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = Environment.GetEnvironmentVariable("STEAM_PROXY_USERNAME"),
                Password = Environment.GetEnvironmentVariable("STEAM_PROXY_PASSWORD"),
            });
        }

        private static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Log.GoogleInfo("Disconnected from Steam");
            isLoggedOn = false;

            if (quitOnDisconnect)
            {
                Environment.Exit(0);
            }

            // Try to reconnect
            Thread.Sleep(TimeSpan.FromSeconds(15));
            steamClient.Connect();
        }

        private static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Log.GoogleInfo(
                    string.Format("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult)
                );
                return;
            }

            Log.GoogleInfo("Logged in");
            isLoggedOn = true;
        }

        private static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Log.GoogleInfo("Logged off");
            isLoggedOn = false;

            if (quitOnDisconnect)
            {
                steamClient.Disconnect();
            }
        }
    }

    internal class DebugListener : IDebugListener
    {
        public void WriteLine(string category, string msg)
        {
            Log.GoogleInfo(string.Format("Debug - {0}: {1}", category, msg));
        }
    }
}