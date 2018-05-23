
using System;
using SteamKit2;
using SteamKit2.Internal; // this namespace stores the generated protobuf message structures
using Newtonsoft.Json;

namespace RiderProjects
{
    internal static class Program
    {
        private static SteamClient _steamClient;
        private static CallbackManager _manager;

        private static SteamUser _steamUser;
        private static MyHandler _myHandler;

        private static bool _isRunning;

        private static void Main(string[] args)
        {
            // create our steamclient instance
            _steamClient = new SteamClient();

            // add our custom handler to our steamclient
            _steamClient.AddHandler(new MyHandler());

            // create the callback manager which will route callbacks to function calls
            _manager = new CallbackManager(_steamClient);

            // get the steamuser handler, which is used for logging on after successfully connecting
            _steamUser = _steamClient.GetHandler<SteamUser>();
            // now get an instance of our custom handler
            _myHandler = _steamClient.GetHandler<MyHandler>();

            // register a few callbacks we're interested in
            // these are registered upon creation to a callback manager, which will then route the callbacks
            // to the functions specified
            _manager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            _manager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);
            _manager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            _manager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);

            // handle our own custom callback
            _manager.Subscribe<MyHandler.MyCallback>(OnMyCallback);

            //manager.Subscribe<SteamApps.PICSChangesCallback>(OnMyCallback);

            _isRunning = true;

            Console.WriteLine("Connecting to Steam...");

            // initiate the connection
            _steamClient.Connect();

            // create our callback handling loop
            while (_isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                _manager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }

        }


        private static void OnConnected(SteamClient.ConnectedCallback callback)
        {
            Console.WriteLine("Connected to Steam! Logging in anon.");
            _steamUser.LogOnAnonymous();
        }

        private static void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            Console.WriteLine("Disconnected from Steam");

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

            Console.WriteLine("Successfully logged on!");

            // at this point, we'd be able to perform actions on Steam

            // for this sample we'll just log off
            _steamUser.LogOff();
        }

        private static void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            Console.WriteLine("Logged off of Steam: {0}", callback.Result);
        }

        private static void OnMyCallback(MyHandler.MyCallback callback)
        {
            // this will be called when our custom callback gets posted
            Console.WriteLine("OnMyCallback: {0}", callback.Result);
        }
    }


    internal class MyHandler : ClientMsgHandler
    {
        // define our custom callback class
        // this will pass data back to the user of the handler
        public class MyCallback : CallbackMsg
        {
            public EResult Result { get; private set; }

            // generally we don't want user code to instantiate callback objects,
            // but rather only let handlers create them
            internal MyCallback(EResult res)
            {
                Result = res;
            }
        }


        // handlers can also define functions which can send data to the steam servers
        public void LogOff(string user, string pass)
        {
            var logOffMessage = new ClientMsgProtobuf<CMsgClientLogOff>(EMsg.ClientLogOff);

            Client.Send(logOffMessage);
        }

        // some other useful function
        public void DoSomething()
        {
            // this function could send some other message or perform some other logic

            // ...
            // Client.Send( somethingElse ); // etc
            // ...
        }

        public override void HandleMsg(IPacketMsg packetMsg)
        {
            // this function is called when a message arrives from the Steam network
            // the SteamClient class will pass the message along to every registered ClientMsgHandler

            // the MsgType exposes the EMsg (type) of the message
            switch (packetMsg.MsgType)
            {
                // we want to custom handle this message, for the sake of an example
                case EMsg.ClientLogOnResponse:
                    HandleLogonResponse(packetMsg);
                    break;

            }
        }

        private void HandleLogonResponse(IPacketMsg packetMsg)
        {
            // in order to get at the message contents, we need to wrap the packet message
            // in an object that gives us access to the message body
            var logonResponse = new ClientMsgProtobuf<CMsgClientLogonResponse>(packetMsg);

            // the raw body of the message often doesn't make use of useful types, so we need to
            // cast them to types that are prettier for the user to handle
            var result = (EResult)logonResponse.Body.eresult;

            // our handler will simply display a message in the console, and then post our custom callback with the result of logon
            Console.WriteLine("HandleLogonResponse: {0}", result);

            // post the callback to be consumed by user code
            Client.PostCallback(new MyCallback(result));
        }
    }
}
