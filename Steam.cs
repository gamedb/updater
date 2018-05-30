using System;
using System.IO;
using SteamKit2;
using System.Threading.Tasks;
using System.Timers;

namespace SteamProxy
{
    public class Steam
    {
        public uint PreviousChangeNumber;

        private bool IsLoggedOn = false;

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
        }

        public void Start()
        {
            Task.Run(() => DoTheThingAsync());
            

            _isRunning = true;
            _steamClient.Connect();


            
            Task.Run(async () =>
            {
                while (_isRunning)
                {
                    _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));

                    if (IsLoggedOn)
                    {
                        _steamApps.PICSGetChangesSince(PreviousChangeNumber, true, true);
                        /await Task.Delay(200);
                    }
                }
            });
        }
        
        public async void DoTheThingAsync()
        {
            Console.Write("DONE2");
        }

        public void UpdateChangeNumber(uint number)
        {
            PreviousChangeNumber = number;

            File.WriteAllText(ChangeFetcher.LastChangeFile, Convert.ToString(number));
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

            Rabbit.Produce(callback);
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