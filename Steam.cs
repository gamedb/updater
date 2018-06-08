using System;
using System.IO;
using SteamKit2;
using Newtonsoft.Json;
using System.Timers;

namespace SteamProxy
{
    public static class Steam
    {
        private const string LastChangeFile = "last-changenumber.txt";

        private static uint previousChangeNumber;
        private static bool isLoggedOn;

        private static SteamClient steamClient;
        private static CallbackManager manager;

        private static SteamUser steamUser;
        public static SteamApps steamApps;

        public static void startSteam()
        {
            // Debug
            DebugLog.AddListener(new DebugListener());
            DebugLog.Enabled = false;

            // Setup client
            steamClient = new SteamClient();
            manager = new CallbackManager(steamClient);

            steamUser = steamClient.GetHandler<SteamUser>();
            steamApps = steamClient.GetHandler<SteamApps>();

            // Callbacks
            manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            manager.Subscribe<SteamApps.PICSChangesCallback>(OnPicsChanges);
            manager.Subscribe<SteamApps.PICSProductInfoCallback>(OnPicsInfo);

            steamClient.Connect();

            var timer1 = new Timer();
            timer1.Elapsed += CheckForChanges;
            timer1.Interval = TimeSpan.FromSeconds(5).TotalMilliseconds;
            timer1.Start();

            var timer2 = new Timer();
            timer2.Elapsed += RunWaitCallbacks;
            timer2.Interval = TimeSpan.FromSeconds(1).TotalMilliseconds;
            timer2.Start();
        }

        private static void CheckForChanges(object obj, EventArgs args)
        {
            if (!steamClient.IsConnected || !isLoggedOn)
            {
                return;
            }

            if (previousChangeNumber == 0 && File.Exists(LastChangeFile))
            {
                previousChangeNumber = uint.Parse(File.ReadAllText(LastChangeFile));
            }
            else if (previousChangeNumber == 0)
            {
                previousChangeNumber = 4500000;
            }

            Console.WriteLine("Checking for changes: " + previousChangeNumber);

            steamApps.PICSGetChangesSince(previousChangeNumber, true, true);
        }

        private static void RunWaitCallbacks(object obj, EventArgs args)
        {
            manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }

        private static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine("Connected");
            steamUser.LogOnAnonymous();
        }

        private static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);
                return;
            }

            Console.WriteLine("Logged in");

            isLoggedOn = true;
        }

        private static void OnPicsChanges(SteamApps.PICSChangesCallback callback)
        {
            if (previousChangeNumber == callback.CurrentChangeNumber)
            {
                return;
            }

            Console.WriteLine("Adding {0} apps, {1} packages to Rabbit"
                , callback.AppChanges.Count, callback.PackageChanges.Count);

            foreach (var key in callback.AppChanges.Values)
            {
                Rabbit.Produce(Rabbit.queueAppIds, key.ID.ToString());
            }

            foreach (var key in callback.PackageChanges.Values)
            {
                Rabbit.Produce(Rabbit.queuePackageIds, key.ID.ToString());
            }

            previousChangeNumber = callback.CurrentChangeNumber;
            File.WriteAllText(LastChangeFile, previousChangeNumber.ToString());
        }

        private static void OnPicsInfo(SteamApps.PICSProductInfoCallback callback)
        {
            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }

        private static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off");
            isLoggedOn = false;
        }

        private static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected from Steam");
            isLoggedOn = false;
            steamClient.Connect();
        }
    }

    internal class DebugListener : IDebugListener
    {
        public void WriteLine(string category, string msg)
        {
            Console.WriteLine("MyListener - {0}: {1}", category, msg);
        }
    }
}