using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
//using static System.Runtime.InteropServices.JavaScript.JSType;
using ShimamuraBot.Classes;
using System.Runtime.InteropServices;
using ShimamuraBot.Web.Api;
using System.Diagnostics;

namespace ShimamuraBot
{
    class Program
    {
        //if you decode it let me know -adachi
        //public const string EASTER_EGG = "GAJ9MDCDIDEAHDTC9D9DEAADTCEAXCHDLAGDEAPC9DFDXCVCWCHDQAJ9HDTC9D9DEAADTCEASBLAADEAUCCDFDVCXCJDTCBDEAHDCDBDXCVCWCHDQAJ9QCIDHDEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCEABDCDKDQAJ9SBLAADEAWCCD9DSCXCBDVCEAIDDDEAPCEA9DXCVCWCHDQAJ9RCWCPCGDXCBDVCEACDIDHDEAHDWCTCEASCPCFDZCBDTCGDGDEAXCBDGDXCSCTCQAJ9RCPCIDGDTCEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCSAJ9GA";
        public static Int16 LoopbackPort = 8087;
        private static WebsocketClient? wss;
        private static string name = "Shimamura";
        public static string? ENVIRONMENT_PATH;

        public static string? HOST;             // <=== base host
        public static string? CLIENT_ID;        // <=== ApplicationID to form basic auth
        public static string? CLIENT_SECRET;    // <=== Client Secret to form basic auth
        public static string? WSS_HOST;         // <=== Host
        public static string? ACCESS_TOKEN;     // <=== Web Token
        public static string? REFRESH_TOKEN;    // <=== Refresh Token
        public static string? USERNAME;         // <=== Username

        /* TO BE SPLIT */
        public static string? TWITCH_HOST;
        public static string? TWITCH_CLIENT_ID;
        public static string? TWITCH_CLIENT_SECRET;
        public static string? TWITCH_ACCESS_TOKEN;
        public static string? TWITCH_REFRESH_TOKEN;

        public static VNyan? vNyan;
        public static WebInterface? WebUI;
        private static Twitch? _Twitch;
        public static bool DEBUGGING_ENABLED = false;
        public static bool LOGGING_ENABLED = false;

        public const string LOG_FILE_PATH = @"shimamura.log";

        //private static SynchronizationContext mainThreadContext; /// This was an attempt to pass context back to the main thread, after using an annymous task runner.
        public static StringBuilder UserInput = new StringBuilder();

        //Modules - Only loading global most likely used.
        /*
         * Buffer -> input=modules -> Pause Main Buffer -> Enter Module While() {} for configuring modules is what I think this was for.
         * TODO: Figure out a better way to configure modules & settings .json without stopping Main. - Though I don't think there is an easy solution, I don't think exec will work across all platforms, and without loading a large ass library to handle a GUI
         */
        public static bool MODULE_CONFIGURATION = false; //will be used to pause buffer output while configuring modules, should I just make a GUI in VB to track their IP address? (I WILL BRING BACK DEAD MEMES)
        public static string? DISCORD_URI = null; // I thinks I can implements this now.


        #region MainLoopMultiThreading_TODO
        //TODO: Refactor this entire fucking piece of shit - Already on it.
        public class MainThread {
            private string name = "MainThread";
            public readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
            private Thread? _mainLoop;
            private Thread? _webSocketThread;
            private WebsocketClient _webSocketClient;
            private CancellationTokenSource _is_cancel_requested = new();
            private CancellationTokenSource? _on_first_stop = new();

            private bool _running { get; set; } = false;
            private int _tick { get; set; } = 0;
            private int _ON_FIRST_CHANCE = 0;


            public bool Running => _is_cancel_requested.IsCancellationRequested; // this is fine for now but I want to change it to something with less overhead when it's called more frequeently.
            public void Start() {
                if (_mainLoop != null && _mainLoop.IsAlive) { Print(name, "Thread tried to start again?", PrintSeverity.Error); return; }

                _mainLoop = new Thread(() => Loop()) { IsBackground = true };
                _mainLoop.Start();
            }

            public void Stop() {
                if(_mainLoop != null && _mainLoop.IsAlive) {
                    _is_cancel_requested.Cancel();
                    _on_first_stop?.Cancel();
                    _mainLoop.Join();

                    //_on_first_stop.Dispose();
                    _is_cancel_requested.Dispose();
                    Interlocked.Exchange(ref _ON_FIRST_CHANCE, 0);
                    Print(name, $"Thread exited. {Environment.CurrentManagedThreadId}", PrintSeverity.Debug);
                } else {
                    Print(name, $"Thread is not running!", PrintSeverity.Error);
                }
            }

            public void Build_Sockets() {
                _webSocketThread = new Thread(() => {
                    _webSocketClient = new WebsocketClient("DEF4928FAB047192F129748E9012804A31", Service.Joystick);
                }) { IsBackground = true };
                _webSocketThread.Start();
            }

            //public static Dictionary<string, long> FiveMillionTimers = new Dictionary<string, long>();
            //Timer mytimer = new Timer(myTimeTicker);

            public MainThread() {
                //Remember to Thread.join() Jackass. 2024
            }

            enum Pikachui {
                A,
                B
            }

            public record tits(
                string Host

                );

            // I'll be judged ONLY IN HELL, SEE YOU THERE FUCKERS.
            public void McSnuffleTesticles() {
                void Hello() {

                }

                bool Moo() {
                    return true;
                }

                void start_WebUI() {
                    if (WebUI != null) WebUI.Start(null); // This has to have a token because it's on a different unmanaged/managed thread.
                }

                if (1 == 1) Hello();
                var a = Moo();

                // How do we start our story, we have 2 little sockets waiting to meet the world
                // First they will need credentials to escape the evil fortress.
                // Then they what the fuck am I doing.

                // First I check if Joystick.tv has a valid webtoken, if it does then I proceede igniting the world on fire.
                // if it doesn't and it has a refreshtoken it attempts a refresh.
                // if the fresh fails due to the token being expired because I'm an asshole
                // it will attempt to reflow the entire thing
                // at the end of each of these steps if there is success it fists the token into JWT class,
                // then connects websocket and webui
                // as you see this can be a bit problematic because it only checks 1 service that uses a webtoken
                // the other integrated service does not use webtokens, what now bitch?

                /*enum Pikachui {
                    A,
                    B
                }*/

                /**/
                void test(tits rec) {
                    string f = rec.Host;
                }

                void OAuthFlow(Pikachui a) {
                tits asdf = new tits("Moo");

                    switch(a) {
                    case Pikachui.A:
                        test(asdf);
                        break;
                    }
                }
            }


            private void Loop() {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try {
                    void DoSomething() {

                    }

                    while (!_is_cancel_requested.IsCancellationRequested) {

                        #region On_First_Chance
                        /* ==      On First Chance is run only once when it gets the first chance to acquire the Channel Username from API.     === */
                        if (Interlocked.CompareExchange(ref _ON_FIRST_CHANCE, 1, 0) == 0) {
                            Interlocked.Exchange(ref _ON_FIRST_CHANCE, 1);

                            _ = Task.Run(async () => {
                                while (!_on_first_stop.IsCancellationRequested) {
                                    // we may not need to remotely retrieve it, also wait for Config.load as well.
                                    if (!string.IsNullOrEmpty(USERNAME)) break;

                                    if (JWT.Valid && !JWT.Expired) {

                                        using (Joystick.API getStream = new()) {
                                            Joystick.API.StreamSettings? settings = await getStream.GetStreamSettingsAsync(true);

                                            if (settings != null) {
                                                USERNAME = settings.username;
                                                Print(name, $"Username acquired, exiting ON_FIRST_CHANCE.", PrintSeverity.Debug);
                                                break;
                                            }
                                        }
                                    } else {
                                        await Task.Delay(1_500);
                                    }
                                }
                                _on_first_stop.Dispose(); // no longer needed it's only for startup.
                            }, _on_first_stop.Token);

                            /* ===== wss handler? ==== */
                            _ = Task.Run(() => {
                            
                            });
                        }
                        #endregion


                        /*_tick++;
                        
                        if(_tick >= 1_000) {
                            Console.WriteLine("Hello, This is tick");
                            _tick = 0;
                        } */
                        //depre - sockets have their own keepalive now.
                        if(stopwatch.ElapsedMilliseconds % 1000 == 0) { // is this an expensive opreation? Idk, why the fuck do I care anymore.
                            if(WebUI != null && WebUI.Open && Interlocked.CompareExchange(ref WebUI._ohno, 0, 0) != 1) {
                                _ = Task.Run( async () => {
                                    Interlocked.Exchange(ref WebUI._ohno, 1);
                                    // WHAT THE FUCK COULD GO WRONG // I agree past me.
                                    bool a = false;
                                    try {
                                        //a = await WebUI.Ping();
                                    } catch (Exception ex) {
                                        new BotException(name, $"Bot could not ping the gloryhole of OBS.", ex);
                                    }
                                    //if (!a) Print($"{name}:WebInterface:OBS:SSE:More:Stuff", $"Shit's dead, yo.", PrintSeverity.Warn);
                                    //else Print($"{name}:WebInterface", "I hate to break this news to you but, your ping was succcesful.", PrintSeverity.Debug);
                                    Interlocked.Exchange(ref WebUI._ohno, 0);
                                });
                            }
                        }

                        if (stopwatch.ElapsedMilliseconds >= 3500) {
                            //Console.WriteLine("Hello, This is tick");
                            //Print("", $"How annoying? {IS_WIN} {Environment.CurrentManagedThreadId}", PrintSeverity.Normal);
                            stopwatch.Restart();
                        }

                        Thread.Sleep(1);
                        //await Task.Delay(1); // Remember we are GOTO, jumping, is why this fails first test.
                    }
                }
                catch (OperationCanceledException) { Print(name, $"Zeds dead, baby!", PrintSeverity.Debug); }
            }

        }
        // End MAIN_LOOP;
        #endregion

        //private static OAuthClient? oAuth;
        private static MainThread ManagedThreads = new MainThread();
        //private static WebInterface Webie = new("http://127.0.0.1");

        [Obsolete("Shits ded still yo")]
        private enum userInputs
        {
            say = 2, //say, msg
            whisper = 3, // whisper, user, msg
            mute = 3, // No idea why mute was 3 args it should be 2
            test = 3, // NO idea what I was using this for.
            cat = 2 //vNyan websocket takes 1 additional arg (websocket payload).
        }

        [Obsolete("Shits ded still yo")]
        private static int? GetEnumValueIfStartsWith(string input) {
            foreach (var enumName in Enum.GetNames(typeof(userInputs))) {
                if (input.StartsWith(enumName)) {
                    userInputs enumValue = (userInputs)Enum.Parse(typeof(userInputs), enumName);
                    return (int)enumValue;
                }
            }
            return null;
        }

        [Obsolete("Shits ded still yo")]
        /// <summary>
        ///  Construct a string[] with a set length based on the command issued by userinput.
        /// </summary>
        /// <param name="input">Console.ReadLine()</param>
        /// <returns>string[] - command, params</returns>
        private static string[] user_InputParser(string input) // this is a mess redo it <--------------------------------------------------------------------------------------
        {
            string[] strings = [];

            int? enumValue = GetEnumValueIfStartsWith(input);
            if (enumValue.HasValue)
            {
                string[] tmp = new string[enumValue.Value]; // e.g. 2 for say => string[2] => [say, msg] 
                strings = input.Split(" ", enumValue.Value);
                if (strings.Length < tmp.Length)
                {
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        if (i > strings.Length - 1)
                            tmp[i] = "";
                        else
                            tmp[i] = strings[i];

                        /*if (string.IsNullOrEmpty(strings[i]))
                                tmp[i] = "";
                        else
                                tmp[i] = strings[i];*/
                    }
                    strings = new string[tmp.Length];
                    strings = tmp;
                }
            } else {
                strings = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            }

            return strings;
        }


        /// <summary>
        ///  The only external connection to send a websocket message, GL HAVE FUN YOU ARE ONLY ABLE TO SEND A MESSAGE BECAUSE OF MY FLOW
        ///  THE FUCKING ILLEST FLOW
        /// </summary>
        /// <param name="action">send_message or die</param>
        /// <param name="msg">String - TextXTASDF</param>
        /// <returns></returns>
        public static Task SendWebSocketMsg(string action, string msg="") => _ = wss?.SendMessage(action, msg) ?? throw new BotException(name, "Could not send message, no socket.");

        /// <summary>
        ///  Entry point
        /// </summary>
        /// <param name="args">Maybe</param>
        /// <returns></returns>
        /// <exception cref="BotException"></exception>
        async static Task Main(string[] args)
        {
            AppContext.SetSwitch("System.Net.DisableTelemetry", true); // WHAT THE FUCK?
            //AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            //Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            //mainThreadContext = SynchronizationContext.Current;

            if (File.Exists(".env")) ENVIRONMENT_PATH = ".env";
            else {
                Print("Environment", $"Could not find the environment file, please specify the path below. Default: .env", PrintSeverity.Warn);
                while(true) {
                    Console.Write("path>");
                    var path = Console.ReadLine();

                    if (path == "exit" || path == "close" || path == "end") Environment.Exit(0);
                    if (File.Exists(path)) { ENVIRONMENT_PATH = path; break; }
                    else Print("Environment", $"Could not find the path: {path}. Please make sure the file path is correct.", PrintSeverity.Error);
                }
            }

            try {
                Config.load();
            } catch {
                // Unrecoverable exception occured (thrown from load) need to exit regardless of BotException or General Exception.
                Print(name, $"Can't recover from error, application needs to close.{Environment.NewLine}Press any key to close.", PrintSeverity.Normal);
                Console.ReadKey();
                return;
            }

            // All Globals have been loaded.
            _ = JWT.Token(); // Store JSON Web Token Payload values for retrieval from other parts of things doing things and things.
            /// Let on FIRST_CHANCE retrieve it.
            //if (JWT.Valid && string.IsNullOrEmpty(USERNAME)) _ = Task.Run(async () => { using (Joystick.API streamSets = new()) { var settings = await streamSets.GetStreamSettingsAsync(true); if (!string.IsNullOrEmpty(settings?.username)) USERNAME = settings.username; } });

            wss = new(JWT.GetChannelIdentifier ?? string.Empty, Service.Joystick); // Set the Channel ID so interaction is possible.
            WebUI = new("127.0.0.1");

            /*oAuth = new OAuthClient(
                HOST!,
                CLIENT_ID!,
                CLIENT_SECRET!,
                "/api/oauth/authorize",
                "/api/oauth/token",
                $"https://127.0.0.1:{LoopbackPort}/auth",
                "code",
                "amCatSorryfortestingallthis",
                OAuthClient.AuthTypes.Basic,
                OAuthClient.Service.Joystick
            );*/

            OAuthClient TwitchOAuth2 = new(
                "https://id.twitch.tv",
                TWITCH_CLIENT_ID!,
                TWITCH_CLIENT_SECRET!,
                "/oauth2/authorize",
                "/oauth2/token",
                $"http://localhost:{LoopbackPort}/auth",
                "code",
                "chat:edit chat:read whispers:read whispers:edit channel:moderate channel:read:redemptions channel:read:subscriptions channel:read:subscriptions channel:read:polls channel:manage:polls channel:read:predictions channel:manage:predictions channel:read:hype_train channel:manage:hype_train channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit:clips channel:moderate channel:read:stream_key channel:read:subscriptions channel:manage:videos channel:read:videos channel:read:clips channel:edit",
                AuthTypes.None,
                Service.Twitch
            );

            Console.Title = $"♥ Shimamura :: 0 ♥{Heart_Purple}"; // just seeing, probably won't work, right? RIGHT?

            if (!ManagedThreads.Running) ManagedThreads.Start();
            //Console.Clear();
            #region Welcome ASCII garbage
            Console.ForegroundColor = ConsoleColor.Cyan;
            string headerBorder = new string('=', Console.WindowWidth);
            Console.WriteLine(headerBorder);
            Console.SetCursorPosition(0, 1);
            Console.Write($"===");
            Console.SetCursorPosition(Console.WindowWidth / 2 - 27, 1);
            Console.Write($"♥ Shimamura Bot ♥ v{Assembly.GetExecutingAssembly().GetName().Version}. Welcome !");
            Console.SetCursorPosition(Console.WindowWidth - 3, 1);
            Console.Write("===");
            Console.SetCursorPosition(0, 2);
            Console.WriteLine(headerBorder);
            Console.SetCursorPosition(1, 4);
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Type \"Help\" for commands, or \"Start\" to start the bot");
            #endregion

            Print(name, $"Successfully loaded environment file.", PrintSeverity.Normal);
            DEBUGGING_ENABLED = true; // keep this until you're done with config. This is for when I launch the program outside of VS.
            while (true) {
                /*var key = Console.ReadKey(intercept: true); //--## A fool's dream. I might come back and try to fix the buffer, until then. I need all components to work.

                if (key.Key == ConsoleKey.Enter) {*/
                    string? input = Console.ReadLine();
                    //string input = UserInput.ToString();
                    //UserInput.Clear();
                    string[] msg = input?.Split(' ', 2) ?? ["noop"];
                //string[] msg = user_InputParser(input);

                switch (msg[0].ToLower()) {
                    case "aa":
                        Print(name, $"Assert {msg[0]}", PrintSeverity.Debug);
                        break;
                    case "mlstop" or "loopstop" or "stoploop" or "killloop" or "becausehwy" or "cause":
                        ManagedThreads.Stop();
                        break;
                    case "sse": // deprecate
                        string[] paramore = msg[1].Split(" ", 2);
                        switch(paramore[0]) {
                            case "start":
                                //Webie.Start();
                                break;
                            case "stop":
                                //Webie.Stop();
                                break;
                            case "send":
                                //Webie.SendSSEAudioAsync("https://adachi.wiki/xeumap/whisper.mp3", 1, false);
                                //Webie.SendSSEVideoAsync("https://cdn.jsdelivr.net/npm/big-buck-bunny-1080p@0.0.6/video.mp4");
                                WebUI.SendSSEImageAsync("https://steamuserimages-a.akamaihd.net/ugc/778494769436587920/675371BED432AF394DB2F145632671082F4779DF/?imw=5000\u0026imh=5000\u0026ima=fit\u0026impolicy=Letterbox\u0026imcolor=%23000000\u0026letterbox=false", 4);
                                break;
                        }
                        break;
                    case "assertsocket":
                        Stopwatch sw = new();
                        sw.Start();
                        if (wss != null && wss.Open) if (await wss.SocketOpen()) { Print(name, $"Time: {sw.ElapsedMilliseconds}", PrintSeverity.Debug); }; // you're fucking stupid Visual Studio.
                        //Print(name, $"Websocket is {(wss == null ? "null" : "not null")}", PrintSeverity.Debug);
                        break;
                    case "fuck":
                        _ = Task.Run(async () => {
                            var succ = await TwitchOAuth2.StartFlowAsync();
                            if(succ != OAuth2States.TokenAcquired)
                                Print("TwitchOAuth2", "Failed", PrintSeverity.Debug);
                            else
                                Print("TwitchOAuth2", "Success", PrintSeverity.Debug);
                        });
                        break;
                    case "getclients":
                        if (WebUI != null) Print(name, $"Clients connected: {WebUI.debug_clients}", PrintSeverity.Debug);
                        break;
                    /* CURRENT WORK =================================================================================== */
                    case "revoke":
                        Print(name, $"This will revoke OAuth2 code and Web Token.{Environment.NewLine}Continue? (y/n) >", PrintSeverity.Warn);
                        bool t = false;
                        while (true) {
                            var b = Console.ReadLine()?.ToLower();
                            if (b == "y" || b == "yes") { t = true; break; } else if (b == "n" || b == "no" || b == "q" || b == "exit") { break; }
                        }
                        if (!t) break;


                        await RequestClosure();

                        _ = Task.Run(async () => {
                            using (OAuthClient oAuth = new(HOST!, CLIENT_ID!, CLIENT_SECRET!,"/api/oauth/authorize", "/api/oauth/token", $"https://127.0.0.1:{LoopbackPort}/auth", "code", "amCatSorryfortestingallthis", AuthTypes.Basic, Service.Joystick)) {
                                var a = await oAuth.Revoke();

                                if (a == OAuth2States.TokenAcquired) {
                                    Print(name, $"OAuth2 flow succeded.", PrintSeverity.Debug);
                                } else {
                                    Print(name, "OAuth2 flow failed.", PrintSeverity.Debug);
                                }
                            }

                            Print(name, "Exiting OAuth2 anonymous Task.", PrintSeverity.Debug);
                        });
                        break;
                    case "set":
                        string[] parAGRAPHS = msg[1].Split(" ", 2);
                        switch (parAGRAPHS[0]) {
                            case "title":
                                _ = Task.Run( async () => {
                                    try {
                                        using (Joystick.API streamSettings = new()) {
                                            await streamSettings.SetTitleAsync(parAGRAPHS[1]);
                                            _ = Logger.LogAsync(name, new string[] { "Stream Title Updated :: ", parAGRAPHS[1] });
                                            Print(name, $"Stream Title Updated: {parAGRAPHS[1]}.", PrintSeverity.Normal);
                                        }
                                    } catch (Exception ex) { new BotException(name, $"Exception happened while setting title.", ex); }
                                });
                                break;
                            case "addwords":
                                _ = Task.Run(async () => { // Todo wait for call back for success and log it.
                                    try {
                                        if (string.IsNullOrEmpty(parAGRAPHS[1])) { throw new BotException(name, "No words were submitted."); }

                                        using (Joystick.API streamSetting = new()) {
                                            List<string> words = new List<string>();
                                            if (parAGRAPHS[1].Contains(","))
                                                words.AddRange(parAGRAPHS[1].Replace(" ", "").Split(','));
                                            else
                                                words.Add(parAGRAPHS[1]);

                                            await streamSetting.SetBannedWordAddAsync(words.ToArray());
                                        }
                                    } catch (Exception ex) {
                                        new BotException(name, "Error adding banned word.", ex);
                                    }
                                });
                                break;
                            case "removewords":
                                _ = Task.Run(async () => { // Todo wait for call back for success and log it.
                                    try {
                                        if (string.IsNullOrEmpty(parAGRAPHS[1])) { throw new BotException(name, "No words were submitted."); }

                                        using (Joystick.API streamSetting = new()) {
                                            List<string> words = new List<string>();
                                            if (parAGRAPHS[1].Contains(","))
                                                words.AddRange(parAGRAPHS[1].Replace(" ", "").Split(','));
                                            else
                                                words.Add(parAGRAPHS[1]);

                                            await streamSetting.SetBannedWordRemoveAsync(words.ToArray());
                                        }
                                    } catch (Exception ex) {
                                        new BotException(name, "Error adding banned word.", ex);
                                    }
                                });
                                break;
                            case "greeting" or "greet":
                                _ = Task.Run(async () => {
                                    try {
                                        using (Joystick.API streamSetting = new()) {
                                            await streamSetting.SetGreetingAsync(parAGRAPHS[1]);
                                            _ = Logger.LogAsync(name, new string[] { "Stream Greeting Updated :: ", parAGRAPHS[1] });
                                            Print(name, $"Stream Greeting Updated: {parAGRAPHS[1]}", PrintSeverity.Debug);
                                        }
                                    } catch (Exception ex) {
                                        new BotException(name, $"Exception occured while trying to set ChatGreeting message.", ex);
                                    }
                                });
                                break;
                        }
                        break;
                    case "get":
                        using (Joystick.API neverevereveverveverveer = new()) {
                            //neverevereveverveverveer.UpdateStreamSettingsAsync("Meow", "Meow, meow [meow]: \\Meow/;}{{}><.!@#$@%$^&*()_", new string[] { "tacos" });
                            var blah = await neverevereveverveverveer.GetStreamSettingsAsync();
                            //neverevereveverveverveer.Dispose();
                            if (blah == null) break;
                            Print("Debug",
                                    $"Username: {blah.username}\n" +
                                    $"Stream Title: {blah.stream_title}\n" +
                                    $"Chat Welcome Message: {blah.chat_welcome_message}\n" +
                                    $"Banned Chat Words: [{string.Join(", ", blah.banned_chat_words ?? [])}]\n" +
                                    $"Device Active: {blah.device_active}\n" +
                                    $"Photo URL: {blah.photo_url}\n" +
                                    $"Live: {blah.live}\n" +
                                    $"Number of Followers: {blah.number_of_followers}", PrintSeverity.Debug
                                );
                        }
                        break;
                    case "test":

                        if (_Twitch == null) {
                            _Twitch = new("", "", "");

                            var asdf = await _Twitch.ConnectAsync();
                            if (asdf) Print(name, $"Connected to Twitch IRC.", PrintSeverity.Normal); else Print(name, $"Did not connect to Twitch IRC.", PrintSeverity.Normal);
                        } else {
                            var asdf = await _Twitch.CloseAsync();
                            if (asdf) {
                                Print(name, $"Socket closed to Twitch IRC.", PrintSeverity.Normal);
                            } else {
                                Print(name, $"Socket.. Not? closed to Twitch IRC", PrintSeverity.Normal);
                            }
                        }
                        break;
                        
                        /*for(int i = 0; i < 20; i++) // This is a test for prize module.
                        {
                            var player = Enum.GetNames(typeof(RandomNames))[new Random().Next(Enum.GetNames(typeof(RandomNames)).Length)];
                            var prizer = Enum.GetNames(typeof(RandomPrizes))[new Random().Next(Enum.GetNames(typeof(RandomPrizes)).Length)];
                            //_ = UpdateRewards(player, prizer, 1);
                            _ = Redeemer(player, prizer);
                        }*/
                        //_ = Modules.ModuleLoader.LoadSettings();
                        ///Print($"{DISCORD_URI}", 0);
                        break;
                    //Modules.DiscordWebhook webhookd = new Modules.DiscordWebhook(DISCORD_URI, "♥ Coding/Cyberpunk ♥", "I'm coding, and then playing Cyberpunk This is a test message n shit \n new line test \r\n linefeed + carriage return test");
                    //_ = webhookd.SendDiscordWebHook();
                    //Print(name, $"Dones {msg[1]}", PrintSeverity.Debug);
                    //break;

                    /*  =============================================================================================================================================== */
                    /*                                 Real Commands.          or something                                                                             */
                    case "discord":
                        if (string.IsNullOrEmpty(DISCORD_URI)) { new BotException($"{name}:Discord", $"No URL to webhook."); return; }

                        Modules.DiscordWebhook discordHook = new(DISCORD_URI);
                        _ = discordHook.SendDiscordWebHookAsync();
                        break;
                    case "testcef":
                        _ = AudioOot.PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.HeyDumb, WebUI);
                        break;
                    case "002":
                        if (WebUI.Open) WebUI.SendSSEImageAsync("https://steamuserimages-a.akamaihd.net/ugc/778494769436587920/675371BED432AF394DB2F145632671082F4779DF/?imw=5000\u0026imh=5000\u0026ima=fit\u0026impolicy=Letterbox\u0026imcolor=%23000000\u0026letterbox=false", 4);
                        break;
                    case "overunder":
                        //start a new game of over/under
                        Modules.OverUnder Game = new Modules.OverUnder("1:4", 50);
                        break;
                    case "whisper":
                        if(wss.Open) _ = Task.Run(() => {
                            string[] _tmp = msg[1].Split(" ", 2);
                            string user = _tmp[0];
                            string message = _tmp[1];

                            _ = wss.SendWhisper("send_whisper", message, user);
                        });
                        break;
                    case "say":
                        if (wss.Open) _ = Task.Run(() => {
                            _ = wss.SendMessage("send_message", msg[1]);
                        });

                        //_ = SendMessage("send_message", new string[] { msg[1], "", "" });
                        break;
                    case "rage" or "eyes":
                        //_ = Redeemer("adachi91", msg[0], true);
                        _ = Redeemer("adachi91", "duck");
                        break;
                    case "tits":
                        _ = Redeemer("", "tits", true, 10, true);
                        break;
                    case "cumdump" or "cum" or "trip":
                        try { // are you fucking happy this is what happens when you find a stranger in the alps. Because you try to convert then forget the arguments.
                            _ = Redeemer("adachi91", msg[0], true, Convert.ToInt32(msg[1]), true);
                        } catch (Exception ex) { new BotException("YOU", "Hey dipshit, you forgot how to count.", ex); }
                        break;
                    case "mute": // TODO: Implement
                        //int msgid;
                        //string[] mutemsg;
                        //string mutemsgid;
                        try {
                            throw new NotImplementedException();
                            //msgid = int.Parse(tits[2]);
                        } catch (Exception e) {
                            new BotException(name, $"Could not parse to int", e);
                        }
                        //mutemsg = wss.getMessage(msgid);
                        //_ = wss.sendMessage("mute_user", new string[] { "", mutemsg[0], mutemsg[1] });
                        break;
                    case "exit" or "quit":
                        if (await RequestClosure()) Print(name, $"All connections closed gracefully.", PrintSeverity.Normal);
                        return;
                    case "start" or "run" or "connect": // fuck you you piec eof hsith I swear to god I'm going to start from scratch.
                        if (JWT.Valid && !JWT.Expired) { // TODO: Check if you can get rid of Expired by short-cir on valid; IF it's not required soemwhere else to check expiration.
                            Print(name, $"User-input 'run' received, attempting to start bot.", PrintSeverity.Debug);
                            if(wss == null)
                                wss = new WebsocketClient(JWT.GetChannelIdentifier ?? throw new BotException(name, "Tried to create a WebSocket instance without channel Id"), Service.Joystick);
                            if (WebUI == null)
                                WebUI = new("127.0.0.1");

                            _ = wss.ConnectAsync();
                            WebUI.Start(null);
                        } else if (JWT.Valid && JWT.Expired && !string.IsNullOrEmpty(REFRESH_TOKEN)) { // This has the potential to throw.
                            /// Condition: The token has expired, we need to request a new one.
                            //_ = Task.Run(async () => {
                            using (OAuthClient oAuth = new(HOST!, CLIENT_ID!, CLIENT_SECRET!, "/api/oauth/authorize", "/api/oauth/token", $"https://127.0.0.1:{LoopbackPort}/auth", "code", "amCatSorryfortestingallthis", AuthTypes.Basic, Service.Joystick)) {
                                Print(name, $"Token expired {GetUnixTimestamp() - (JWT.GetExpiration ?? -1)} seconds ago, attempting to refresh.", PrintSeverity.Normal);

                                var err = await oAuth.RefreshToken();

                                if (err == OAuth2States.TokenAcquired) {
                                    await JWT.Token(); // STUFFED

                                    Print(name, $"Token was successfully refreshed. New expiration time: {(JWT.GetExpiration ?? -1) - GetUnixTimestamp()} seconds. ☺ ♥ (^o^  )\\", PrintSeverity.Debug);
                                    if (JWT.Valid && !JWT.Expired) {
                                        // Delegate _ = wss.ConnectAsync(); back to the main thread
                                        //mainThreadContext?.Send(_ => wss.ConnectAsync().GetAwaiter().GetResult(), null); //GPT read on what is happening.
                                        Print(name, $"Token was renewed, please type 'run' again to start.", PrintSeverity.Normal);
                                        wss = new(JWT.GetChannelIdentifier ?? throw new BotException(name, "Tried to create a WebSocket instance without channel Id"), Service.Joystick);
                                        _ = wss.ConnectAsync();
                                    }
                                } else if (err == OAuth2States.ReauthorizationRequired) {
                                    var reflow = await oAuth.Revoke();

                                    if (reflow == OAuth2States.TokenAcquired) {
                                        await JWT.Token(); // STUFFED
                                        Print(name, $"Reauthorization of resources completed. State {reflow}", PrintSeverity.Normal);
                                        wss = new(JWT.GetChannelIdentifier ?? throw new BotException(name, "Tried to create a WebSocket instance without channel Id"), Service.Joystick);
                                        _ = wss.ConnectAsync();
                                    } else
                                        Print(name, $"Could not reauthorize resource. State: {reflow}", PrintSeverity.Warn);
                                } else {
                                    Print(name, $"Could not refresh token. OAuth2 State: {err}", PrintSeverity.Warn);
                                }
                            }
                            //});
                        } else {
                            _ = Task.Run(async () => {
                                using (OAuthClient oAuth = new(HOST!, CLIENT_ID!, CLIENT_SECRET!, "/api/oauth/authorize", "/api/oauth/token", $"https://127.0.0.1:{LoopbackPort}/auth", "code", "amCatSorryfortestingallthis", AuthTypes.Basic, Service.Joystick)) {
                                    var state = await oAuth.StartFlowAsync();
                                    switch (state) {
                                        case OAuth2States.ReauthorizationRequired:
                                            var reflow = await oAuth.Revoke();

                                            if (reflow == OAuth2States.TokenAcquired)
                                                Print(name, $"Reauthorization of resources completed. State {reflow}", PrintSeverity.Normal);
                                            else
                                                Print(name, $"Could not reauthorize resource. State: {reflow}", PrintSeverity.Warn);
                                            break;
                                        default:
                                            Print(name, $"Could not complete the flow. OAuth2 State: {state}", PrintSeverity.Normal);
                                            break;
                                    }
                                }
                                // TODO: Add a way to connect after successful OAuth flow.
                            });
                        }
                        break;
                    case "stop":
                        if (await RequestClosure()) Print(name, $"Bot stopped.", PrintSeverity.Normal); else Print(name, $"Nothing running.", PrintSeverity.Normal);
                        break;
                    case "exp":
                        if (JWT.Valid && !JWT.Expired) //OAuthClient.checkJWT())
                            Print(name, $"Token expires in {(JWT.GetExpiration ?? -1) - GetUnixTimestamp()} seconds", PrintSeverity.Debug);
                        else
                            Print(name, $"Token is either invalid or expired.", PrintSeverity.Debug);
                        break;
                    case "resetenv":
                        //Print(name, $"WARNING This will reset all values in your .env file. Are you sure you wish to proceed? (Y/N)", PrintSeverity.Warn);
                        ConsoleColor current = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"WARNING This will reset all values in your .env file. Are you sure you wish to proceed? (Y/N)> ");
                        Console.ForegroundColor = current;
                        while (true) {
                            var _confirm = Console.ReadLine()?.ToLower() ?? "";

                            if (_confirm == "y" || _confirm == "yes") { await FlushToDisk(true); Print(name, $".env has been reset to defaults.", PrintSeverity.Normal); break; } else if (_confirm == "n" || _confirm == "no") { Print(name, $"Cancelled reset.", PrintSeverity.Normal); break; } else break;
                        }
                        break;
                    case "logging":
                        LOGGING_ENABLED = !LOGGING_ENABLED;
                        _ = Task.Run(() => { _ = FlushToDisk(); });
                        if (LOGGING_ENABLED) Print(name, $"Logging is now enabled.", PrintSeverity.Normal); else Print(name, $"Logging is now disabled.", PrintSeverity.Normal);
                        break;
                    case "help":
                        Print("Help", "Command list", PrintSeverity.Normal);
                        Print("", $"start - Starts the bot", PrintSeverity.Normal);
                        Print("", $"stop - Stops the bot", PrintSeverity.Normal);
                        Print("", $"exp - Shows how many seconds are left until your token expires", PrintSeverity.Normal);
                        Print("", $"logging - Toggle logging on/off", PrintSeverity.Normal);
                        Print("", $"resetenv - Resets the .env file to all default values (you will have to fill in the file again).", PrintSeverity.Normal);
                        Print("", $"exit or quit - Exits the program gracefully", PrintSeverity.Normal);
                        Print("", $"say - Usage: say <msg>", PrintSeverity.Normal);
                        Print("", $"whisper - Usage: <Username> <Message>", PrintSeverity.Normal);

                        Print("", $"===== Modules ====", PrintSeverity.Normal);

                        Print("", $"overunder - not finished", PrintSeverity.Normal);
                        Print("", $"rage - vibrate?", PrintSeverity.Normal);
                        Print("", $"eyes - smol eyes", PrintSeverity.Normal);
                        Print("", $"tits - expose breasts", PrintSeverity.Normal);
                        Print("", $"trip - trippy", PrintSeverity.Normal);
                        Print("", $"cum  - liquid overhead", PrintSeverity.Normal);
                        break;
                    default:
                        Print(name, "Invalid command - use help for list of commands", PrintSeverity.Normal);
                        break;
                }
                // pipedream
                /*} else if(key.Key == ConsoleKey.Backspace) {
                    if(UserInput.Length>=0) UserInput.Remove(UserInput.Length -1, 1);
                    //if(Console.ReadLine() != $">{UserInput.ToString()}")
                    Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($">{UserInput.ToString()}");
                } else {
                    UserInput.Append(key.Key);
                    //if (Console.ReadLine() != $">{UserInput.ToString()}")
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($">{UserInput.ToString()}");
                }*/
            }
        }


        /// <summary>
        ///  Shuts down all external program connections.
        /// </summary>
        /// <remarks>Should be checked if <b>True</b> is returned and invoke some message.</remarks>
        /// <returns><see cref="bool"/> Action happened</returns>
        private static async Task<bool> RequestClosure() {
            bool wss_closed = false;
            bool webui_closed = false;

            try {
                // Close WebSocket
                if (wss != null && wss.Open) {
                    await wss.CloseAsync();
                    wss_closed = true;
                } else if(wss != null && !wss.Open) {
                    wss_closed = true;
                }

                // Make sure there is no lingering API
                //To be implemented, LOL YOU CANT EVEN GET IT FUCKING WORKING
                /// Jokes on you bitch, I did get it working.
                if (WebUI != null && WebUI.Open) {
                    await WebUI.Stop(); // Thrown here because token was _cts was cancelled, _socket was Stop()'d but it still accepted a connection?! HOW
                    webui_closed = true;
                } else if(WebUI != null && !WebUI.Open) {
                    webui_closed = true;
                }

            } catch (Exception ex) { new BotException(name, "Unhandled exception.", ex); }

            return (wss_closed && webui_closed);
        }

        [Obsolete("The fuck?")]
        public static async Task<bool> onload_Token(string? Token, string RefreshToken="")
        {
            if(!string.IsNullOrEmpty(Token))
            {
                Console.WriteLine($"Received Tokenstring {Token.Substring(1, 3)}");
                return false;
            }
            ACCESS_TOKEN = Token ?? string.Empty;
            REFRESH_TOKEN = RefreshToken;
            var _success = await JWT.Token();

            return _success;
        }

        [Obsolete("The fuck?")]
        // this whole fucking thing is pointless and a waste of time, gj.
        class UserInputHelper
        {
            string _cmd { get; set; }
            List<string> _input { get; set; }
            
            public class Parameters {
                public string Message { get; set; } // Whisper, Chat Messsage
                public string Message_ID { get; set; } // Mute, Block
                public string Username { get; set; } // Whisper, Unmute
                public string Module { get; set; } // vNyan => redeemer class / ?
                public string Module_Message { get; set; } // Assuming websocket or something I have no idea
            }

            public UserInputHelper(string whatevertheusertyped) // as of right now only 1 input takes an additional arg +2
            {
                _input = new List<string>(whatevertheusertyped.Split(' ')); //[say ]abcdefg hijklmno pqrstuv wxyz
                _cmd = _input[0];
                _input.RemoveAt(0);
                Parameters Params = new Parameters();

                switch(_cmd) {
                    case "whisper":
                        Params.Username = _input[0];
                        Params.Message = string.Join(" ", _input.GetRange(1, _input.Count - 1));
                        break;
                    default:

                        break;
                }

                //options: either switch it or create invididual methods
            }

            public void getsendwhisper(List<string> input) {
                string message = string.Join(" ", input.GetRange(1, input.Count - 1));
            }
        }

        //de..precated? I think I shutdown manually but this is a good idea.
        private static async Task<bool> WaitForClosures()
        {//to even start I need to have a pool of resources to check list down to make sure are closed and disposed or at least gracefully closed.
            await Task.Delay(100);
            throw new NotImplementedException();
            //return true;
        }


        /// <summary>
        /// Call on every type of termination to make sure that sockets, servers, etc are all shut down properly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClose(object sender, EventArgs e)
        {

        }

        private static async Task CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            await Task.Delay(100);
            Console.WriteLine("Exiting process...");

            //var stopped = await Task.Start(() => WaitForClosure());
            //ExitEvent.Set();
            ManagedThreads.ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Canceling process...");
            e.Cancel = true;
            ManagedThreads.ExitEvent.Set();
        }
    }
}
