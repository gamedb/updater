using System;
using System.IO;
using SteamKit2;
using Newtonsoft.Json;

namespace SteamProxy
{
    public class Steam
    {
        private const string LastChangeFile = "last-changenumber.txt";

        private uint previousChangeNumber;
        private bool isLoggedOn = false;

        private readonly SteamClient steamClient;
        private readonly CallbackManager manager;

        private readonly SteamUser steamUser;
        private readonly SteamApps steamApps;

        public Steam()
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
        }

        public void Connect()
        {
            steamClient.Connect();
        }

        public void GetApp(uint id)
        {
            steamApps.PICSGetProductInfo(id, null, false);
        }

        public void CheckForChanges(object obj, EventArgs args)
        {
            if (!steamClient.IsConnected || !isLoggedOn)
            {
                return;
            }

            Console.WriteLine("Checking for changes");

            if (previousChangeNumber == 0 && File.Exists(LastChangeFile))
            {
                previousChangeNumber = uint.Parse(File.ReadAllText(LastChangeFile));
            }
            else if (previousChangeNumber == 0)
            {
                previousChangeNumber = 4500000;
            }

            steamApps.PICSGetChangesSince(previousChangeNumber, true, true);
        }

        public void RunWaitCallbacks(object obj, EventArgs args)
        {
            manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            steamUser.LogOnAnonymous();
            Console.WriteLine("Connected");
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);
                return;
            }

            Console.WriteLine("Logged in");
        }

        private void OnPicsChanges(SteamApps.PICSChangesCallback callback)
        {
            if (previousChangeNumber == callback.CurrentChangeNumber)
            {
                return;
            }

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

        private void OnPicsInfo(SteamApps.PICSProductInfoCallback callback)
        {
            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected");
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