using System;
using Newtonsoft.Json;
using SteamKit2;

namespace SteamProxy
{
    public class Steam
    {
        public uint PreviousChangeNumber;

        private SteamClient _steamClient;
        private CallbackManager _manager;

        private SteamUser _steamUser;
        private SteamApps _steamApps;


        private bool _isRunning;

        public Steam()
        {
            DebugLog.AddListener(new DebugListener());
            DebugLog.Enabled = false;

            _steamClient = new SteamClient();
            _manager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamApps = _steamClient.GetHandler<SteamApps>();

            // Callbacks
            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _manager.Subscribe<SteamApps.PICSChangesCallback>(OnPicsChanges);
            _manager.Subscribe<SteamApps.PICSProductInfoCallback>(OnPicsInfo);

            while (_isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        public void Connect()
        {
            _isRunning = true;
            _steamClient.Connect();
        }

        private void OnConnected(SteamClient.ConnectedCallback callback)
        {
            _steamUser.LogOnAnonymous();
            Console.WriteLine("Connected");
        }

        private void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);
                _isRunning = false;
                return;
            }

            Console.WriteLine("Logged in");

            _steamApps.PICSGetChangesSince(4484615, true, true);

            //_steamUser.LogOff();
        }

        private void OnPicsChanges(SteamApps.PICSChangesCallback callback)
        {
            foreach (var key in callback.AppChanges.Values)
            {
                _steamApps.PICSGetProductInfo(key.ID, null, false);
            }

            foreach (var key in callback.PackageChanges.Values)
            {
                _steamApps.PICSGetProductInfo(null, key.ID, false);
            }

            var json = JsonConvert.SerializeObject(callback);
            Rabbit.Produce(json);
        }

        private void OnPicsInfo(SteamApps.PICSProductInfoCallback callback)
        {
            Console.WriteLine("x");
//            Console.WriteLine(callback);
        }

        private void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        private void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected");
            _isRunning = false;
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