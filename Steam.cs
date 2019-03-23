using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SteamKit2;
using Updater.Consumers;
using static SteamKit2.SteamApps;
using Exception = System.Exception;

namespace Updater
{
    public static class Steam
    {
        public const String LastChangeFile = "last-changenumber.txt";

        public static Boolean quitOnDisconnect;
        public static UInt32 previousChangeNumber;
        public static Boolean isLoggedOn;

        public static SteamClient steamClient;
        public static CallbackManager manager;

        public static System.Timers.Timer timer1;
        public static System.Timers.Timer timer2;

        public static SteamUser steamUser;
        public static SteamApps steamApps;
        public static SteamFriends steamFriends;

        private static readonly HttpClient httpClient = new HttpClient();

        public static void startSteam(Boolean checkChanges, Boolean debug)
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

            steamClient.Connect();

            timer1 = new System.Timers.Timer();
            timer1.Elapsed += RunWaitCallbacks;
            timer1.Interval = TimeSpan.FromSeconds(Config.isLocal() ? 1 : 10).TotalMilliseconds;
            timer1.Start();

            if (checkChanges)
            {
                timer2 = new System.Timers.Timer();
                timer2.Elapsed += CheckForChanges;
                timer2.Interval = TimeSpan.FromSeconds(Config.isLocal() ? 5 : 60).TotalMilliseconds;
                timer2.Start();
            }
        }

        private static void RunWaitCallbacks(Object obj, EventArgs args)
        {
            timer1.Stop();
            manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            timer1.Start();
        }

        private static async void CheckForChanges(Object obj, EventArgs args)
        {
            timer2.Stop();
            try
            {
                // Check logged in
                if (steamClient.IsConnected && isLoggedOn)
                {
                    // Get the last change ID
                    if (previousChangeNumber == 0 && File.Exists(LastChangeFile))
                    {
                        var contents = File.ReadAllText(LastChangeFile);
                        previousChangeNumber = contents == "" ? 0 : UInt32.Parse(contents);
                    }

                    // Get latest changes. If more than 5000, returns 0
                    var JobID = steamApps.PICSGetChangesSince(previousChangeNumber, true, true);
                    var callback = await JobID;

                    if (previousChangeNumber < callback.CurrentChangeNumber)
                    {
                        Log.Info(
                            $"{callback.CurrentChangeNumber - callback.LastChangeNumber:N0} changes, {callback.AppChanges.Count} apps, {callback.PackageChanges.Count} packages"
                        );

                        // Save apps
                        foreach (var appID in callback.AppChanges.Keys)
                        {
                            var appPayload = AppMessage.create(appID);
                            AbstractConsumer.Produce(AbstractConsumer.queue_cs_apps, appPayload);
                        }

                        // Save packages
                        foreach (var packageID in callback.PackageChanges.Keys)
                        {
                            var packagePayload = PackageMessage.create(packageID);
                            AbstractConsumer.Produce(AbstractConsumer.queue_cs_packages, packagePayload);
                        }

                        // Save changes
                        var changePayload = ChangeMessage.create(callback.CurrentChangeNumber, callback);
                        AbstractConsumer.Produce(AbstractConsumer.queue_go_changes, changePayload);

                        // Update change number
                        previousChangeNumber = callback.CurrentChangeNumber;
                        File.WriteAllText(LastChangeFile, previousChangeNumber.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed checking for changes - " + ex.Message);
            }
            finally
            {
                timer2.Start();
            }
        }

        private static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Log.Info("Connected to Steam");
            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = Config.steamUsername,
                Password = Config.steamPassword
            });
        }

        private static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Log.Info("Disconnected from Steam");
            isLoggedOn = false;

            if (quitOnDisconnect)
            {
                Environment.Exit(0);
            }

            // Try to reconnect
            Thread.Sleep(TimeSpan.FromSeconds(10));
            steamClient.Connect();
        }

        private static void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {
            if (callback.Result != EResult.OK)
            {
                Log.Error($"Unable to logon to Steam: {callback.Result} / {callback.ExtendedResult}");
                return;
            }

            Log.Info("Logged in");
            isLoggedOn = true;
        }

        private static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Log.Info("Logged off");
            isLoggedOn = false;

            if (quitOnDisconnect)
            {
                steamClient.Disconnect();
            }
        }
    }

    internal class DebugListener : IDebugListener
    {
        public void WriteLine(String category, String msg)
        {
            Log.Debug($"Debug - {category}: {msg}");
        }
    }

    public class ChangeMessage
    {
        [JsonProperty(PropertyName = "id")]
        public UInt32 ID;

        // ReSharper disable once NotAccessedField.Global
        public PICSChangesCallback PICSChanges;

        public static BaseMessage create(UInt32 id, PICSChangesCallback pics = null)
        {
            return new BaseMessage
            {
                Message = new ChangeMessage
                {
                    ID = id,
                    PICSChanges = pics
                }
            };
        }
    }
}