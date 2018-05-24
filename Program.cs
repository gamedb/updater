using System;
using SteamKit2;
using SteamKit2.Internal; // this namespace stores the generated protobuf message structures
using Newtonsoft.Json;

namespace RiderProjects
{
    internal class DebugListener : IDebugListener
    {
        public void WriteLine(string category, string msg)
        {
            Console.WriteLine("MyListener - {0}: {1}", category, msg);
        }
    }

    internal static class Program
    {
        private static SteamClient _steamClient;
        private static SteamUser _steamUser;
        private static SteamApps _steamApps;

        private static CallbackManager _manager;

        private static bool _isRunning;

        private static void Main(string[] args)
        {
            DebugLog.AddListener(new DebugListener());
            DebugLog.Enabled = true;

            _steamClient = new SteamClient();
            _manager = new CallbackManager(_steamClient);

            _steamUser = _steamClient.GetHandler<SteamUser>();
            _steamApps = _steamClient.GetHandler<SteamApps>();

            // Callbacks
            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            _manager.Subscribe<SteamApps.PICSChangesCallback>(OnPicsChange);

            // Connect
            _isRunning = true;
            _steamClient.Connect();

            // create our callback handling loop
            while (_isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }
        }

        private static void OnPicsChange(SteamApps.PICSChangesCallback callback)
        {
            Console.WriteLine("x");
            Console.WriteLine(JsonConvert.SerializeObject(callback));
        }

        private static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine("Connected");
            _steamUser.LogOnAnonymous();
        }

        private static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected");
            _isRunning = false;
        }

        private static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                if (callback.Result == EResult.AccountLogonDenied)
                {
                    // if we recieve AccountLogonDenied or one of it's flavors (AccountLogonDeniedNoMailSent, etc)
                    // then the account we're logging into is SteamGuard protected
                    // see sample 6 for how SteamGuard can be handled

                    Console.WriteLine("Unable to logon to Steam: This account is SteamGuard protected.");

                    _isRunning = false;
                    return;
                }

                Console.WriteLine("Unable to logon to Steam: {0} / {1}", callback.Result, callback.ExtendedResult);

                _isRunning = false;
                return;
            }

            Console.WriteLine("Logged in");

            //_steamUser.LogOff();
        }

        private static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }
    }
}