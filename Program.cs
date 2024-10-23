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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShimamuraBot
{
    class Program
    {
        //if you decode it let me know -adachi
        //public const string EASTER_EGG = "GAJ9MDCDIDEAHDTC9D9DEAADTCEAXCHDLAGDEAPC9DFDXCVCWCHDQAJ9HDTC9D9DEAADTCEASBLAADEAUCCDFDVCXCJDTCBDEAHDCDBDXCVCWCHDQAJ9QCIDHDEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCEABDCDKDQAJ9SBLAADEAWCCD9DSCXCBDVCEAIDDDEAPCEA9DXCVCWCHDQAJ9RCWCPCGDXCBDVCEACDIDHDEAHDWCTCEASCPCFDZCBDTCGDGDEAXCBDGDXCSCTCQAJ9RCPCIDGDTCEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCSAJ9GA";
        public static int LoopbackPort = 8087;
        private static WebsocketClient wss = new WebsocketClient();

        public static string HOST = null;
        public static string CLIENT_ID = null;
        public static string CLIENT_SECRET = null;
        public static string WSS_HOST = null;
        public static string WSS_GATEWAY;
        public static string ACCESS_TOKEN; // B64 token
        public static string APP_JWT; //bearer interact outside of message/events
        public static long APP_JWT_EXPIRY;
        public static string APP_JWT_REFRESH;
        public static string ENVIRONMENT_PATH;
        public static string CHANNELGUID = null;
        public static VNyan vNyan = null;
        public static bool IS_WIN = true; // I'm so sorry *Nix and MacOS users. I simply don't want to include 3rd party libraries outside of .net :| TODO: Figure out ALSA api

        public const string HISTORY_PATH = @"shimamura.log";
        public static bool LOGGING_ENABLED = false;

        //Modules - Only loading global most likely used.
        public static string NOV_M = string.Empty;
        public static bool MODULE_CONFIGURATION = false; //will be used to pause buffer output while configuring modules, should I just make a GUI in VB to track their IP address? (I WILL BRING BACK DEAD MEMES)
        public static string DISCORD_URI = null;


        public static System.Timers.Timer TimingBelt = new System.Timers.Timer(1000); /*TimingBelt = new Timer((cb) => { 
            
        }, new { a = "" }, Timeout.Infinite, 500);*/

        private static async void TimerTimy(object sender, ElapsedEventArgs e) {
            //CheckJWT
            //Do any other checks
            //check Ping timer?
            //stuff like that.
            await Task.Run(() => {
                /*for(int i =0; i<100; i++)
                Console.WriteLine("fuck");*/
            });
        }

        #region MainLoopMultiThreading_TODO
        //private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        //public static CancellationTokenSource ShutdownToken = new CancellationTokenSource();

        //TODO: Refactor this entire fucking piece of shit
        public class MainThread
        {
            public readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
            public CancellationTokenSource isExiting = new CancellationTokenSource();
            public CancellationToken cancellationToken;
            private bool Started { get; set; } = false;

            public bool Running() { return Started; }
            public void Run() { Started = true; }
            public void Stop() { Started = false; }


            //I'm actually going to refactor this entire section it's going to call to MainLoop.acecssor/method

            public static Dictionary<string, long> FiveMillionTimers = new Dictionary<string, long>();
            //Timer mytimer = new Timer(myTimeTicker);

            public MainThread()
            {
                cancellationToken = isExiting.Token;
                //MainLoop.Start();
                //Remember to Thread.join() Jackass. 2024
            }

            //mainloop shit below this line turn back you don't want to die by reading what's below.
            //private static HTTPServer server;
            public void Start() { MainLoop.Start(new { isExiting = cancellationToken }); }
            //public void Touchy() { tempWebserver.Stop(); }

            private Thread MainLoop = new Thread((object obj) => { //DOES THIS SHIT EVEN EXECUTE?!?!?
                //There are a lot of off-side threads running taskes such as tcp connections
                //it's pretty frustrating but there is very little "off the shelf" event connection to HTTPListener and I'm assuming HTTPClient,
                //though I do imagine HTTPClient will have SOME events to hook into and monitor, why is HTTPListener a little bitch? idk.

                //Manage, Monitor, Handle different aspects of the program while waiting for user input
                var isExiting = (CancellationToken)obj;

                //FiveMillionTimers.Add("Apples", 023985723985);

                /*
                 * init -> Thread.MainLoop(obj).Start() -> OAuth instance(ML) -> HTTPServer start(ML) -> Code populated -> HTTPServer GetToken(ML) ->
                 * > TokenManager(ML)
                 * OH that's what client does I think.
                 */

                Thread.Sleep(1000);
                //shutdownreceive
                Console.WriteLine("hi");
                while (!isExiting.IsCancellationRequested)
                {
                Console.WriteLine("hi2");
                    var f = HOST;
                    Print($"MainLoop says hi!", 0);
                    //Logger.appendFile("");

                    Thread.Sleep(100);
                }
                Print($"Cancellation of MainLoop executed. Terminating loop. Goodbye.", 0);

            });
        }
        #endregion

        private static OAuthClient oAuth;
        private static MainThread MainLoop = new MainThread();

        private enum userInputs
        {
            say = 2,
            whisper = 3,
            mute = 3,
            test = 3,
            cat = 2
        }


        private static int? GetEnumValueIfStartsWith(string input) {
            foreach (var enumName in Enum.GetNames(typeof(userInputs))) {
                if (input.StartsWith(enumName)) {
                    userInputs enumValue = (userInputs)Enum.Parse(typeof(userInputs), enumName);
                    return (int)enumValue;
                }
            }
            return null;
        }


        private static string[] user_InputParser(string input)
        {
            string[] strings = null;

            int? enumValue = GetEnumValueIfStartsWith(input);
            if (enumValue.HasValue)
            {
                string[] tmp = new string[enumValue.Value];
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

        public static async Task SendMessage(string action, params string[] dparams) {
            try {
                await wss.sendMessage(action, true, dparams);
            } catch (BotException) { } catch(Exception ex) {
                new BotException("Websocket", "Unhandled exception", ex);
            }
        }

        /// <summary>
        ///  Entry point
        /// </summary>
        /// <param name="args">Maybe</param>
        /// <returns></returns>
        /// <exception cref="BotException"></exception>
        async static Task Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;



            if (File.Exists(".env")) ENVIRONMENT_PATH = ".env";
            else {
                Print($"[Environment]: Could not find the environment file, please specify the path below. Default: .env", 2);
                while(true) {
                    Console.Write("path>");
                    var path = Console.ReadLine();

                    if (path == "exit" || path == "close" || path == "end") Environment.Exit(0);
                    if (File.Exists(path)) { ENVIRONMENT_PATH = path; break; }
                    else Print($"[Environment]: Could not find the path: {path}. Please make sure the file path is correct.", 3);
                }
            }

            try { envManager.load(); wss.updateshit(); }
            catch (BotException) { } catch (Exception ex) {
                Print($"[Environment]: {ex}", 3);
            }

            oAuth = new OAuthClient(
                HOST,
                CLIENT_ID,
                CLIENT_SECRET,
                "/api/oauth/authorize",
                "/api/oauth/token",
                $"https://127.0.0.1:{LoopbackPort}/auth",
                "bot"
            );

            Console.Title = "♥ Shimamura :: 0 ♥";

            TimingBelt.Enabled = true;
            TimingBelt.Elapsed += TimerTimy;

            if (!MainLoop.Running()) MainLoop.Run();
            Console.Clear();
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

            ///Print($"[.env]: Host: {HOST}", 0);
            ///Print($"[.env]: ID: {CLIENT_ID}", 0);
            ///Print($"[.env]: Secret: {CLIENT_SECRET}", 0);
            ///Print($"[.env]: WSS: {WSS_HOST}", 0);
            ///Print($"[.env]: Token: {ACCESS_TOKEN}", 0);
            ///Print($"[.env]: Gateway: {GATEWAY_IDENTIFIER}", 0);
            ///Print($"[.env]: Refresh: {APP_JWT_REFRESH}", 0);
            Print($"[environment]: Successfully loaded environment file.", 1);

            while (true) {
                string input = Console.ReadLine();
                /*var msg = "";
                var user = "";
                if(input.StartsWith("etest")) {
                    var tmp = input.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries);
                    input = tmp[0];
                    msg = tmp[1];
                }

                string[] tits;
                if (input.ToLower().StartsWith("say"))
                {
                    tits = input.Split(" ", 2, StringSplitOptions.RemoveEmptyEntries);
                    input = tits[0];
                    msg = tits[1];
                }
                else if (input.ToLower().StartsWith("whisper"))
                {
                    tits = input.Split(" ", 3, StringSplitOptions.RemoveEmptyEntries);
                    input = tits[0];
                    user = tits[1];
                    msg = tits[2];
                }*/
                string[] msg = user_InputParser(input);
                /*else
                {
                    tits = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    input = tits[0];

                    string[] tmptits = new string[tits.Length - 1];

                    for (int i = 0; i < tits.Length; i++)
                    {
                        if (i == 0)
                            continue;
                        else
                            tmptits[i - 1] = tits[i];
                    }
                    tits = tmptits;
                }*/

                switch (msg[0].ToLower()) {
                    case "":
                        Print("", 0);
                        break;
                    case "test":
                        /*for(int i = 0; i < 20; i++)
                        {
                            var player = Enum.GetNames(typeof(RandomNames))[new Random().Next(Enum.GetNames(typeof(RandomNames)).Length)];
                            var prizer = Enum.GetNames(typeof(RandomPrizes))[new Random().Next(Enum.GetNames(typeof(RandomPrizes)).Length)];
                            //_ = UpdateRewards(player, prizer, 1);
                            _ = Redeemer(player, prizer);
                        }*/
                        //_ = Modules.ModuleLoader.LoadSettings();
                        ///Print($"{DISCORD_URI}", 0);
                        break;
                        Modules.DiscordWebhook webhookd = new Modules.DiscordWebhook(DISCORD_URI, "♥ Coding/Cyberpunk ♥", "I'm coding, and then playing Cyberpunk This is a test message n shit \n new line test \r\n linefeed + carriage return test");
                        _ = webhookd.SendDiscordWebHook();
                        Print($"[System]: Dones {msg[1]}", 0);
                        break;
                    case "overunder":
                        //start a new game of over/under
                        Modules.OverUnder Game = new Modules.OverUnder("tits");
                        break;
                    case "whisper":
                        _ = SendMessage("send_whisper", new string[] { msg[2], msg[1], "" });
                        break;
                    case "say":
                        _ = SendMessage("send_message", new string[] { msg[1], "", "" });
                        break;
                    case "rage" or "eyes":
                        _ = Redeemer("adachi91", msg[0], true);
                        break;
                    case "tits":
                        _ = Redeemer("", "tits", true, 10, true);
                        break;
                    case "cumdump" or "cum" or "trip":
                        _ = Redeemer("adachi91", msg[0], true, Convert.ToInt32(msg[1]), true);
                        break;
                    case "mute":
                        //int msgid;
                        //string[] mutemsg;
                        //string mutemsgid;
                        try {
                            throw new NotImplementedException();
                            //msgid = int.Parse(tits[2]);
                        } catch (Exception e) {
                            new BotException("System", $"Could not parse to int", e);
                        }
                        //mutemsg = wss.getMessage(msgid);
                            //_ = wss.sendMessage("mute_user", new string[] { "", mutemsg[0], mutemsg[1] });
                        break;
                    case "exit" or "quit":
                        if (wss.Open())
                            await wss.Close(-1);
                        return;
                    case "start" or "run":
                        if (OAuthClient.checkJWT()) {
                            if (!wss.Open())
                                _ = wss.Connect();
                        } else {
                            HTTPServer tempWebserver = new HTTPServer(oAuth); //TODO: make sure it has a proper timeout since it's no longer interactable.
                            var _oauthComplete = await tempWebserver.Start();
                            if (_oauthComplete) {
                                tempWebserver = null;//free up for GC
                                if (!wss.Open())
                                    _ = wss.Connect();
                            }
                        }
                        break;
                    case "stop":
                        if (wss.Open()) {
                            Print($"[Shimamura]: Stopping bot...", 1);
                            try {
                                _ = wss.Close(-1);
                            } catch (BotException) {} catch (Exception e) {
                                new BotException("Websocket", "Unhandled exception", e);
                            }
                        } else
                            Print($"[Shimamura]: Bot is not currently active!", 2);
                        break;
                    case "listen"://PRUNE AFTER FLOW HAS BEEN COMPLETE.
                        //_ = tempWebserver.Start();
                        break;
                    case "stoplisten"://PRUNE  AFTER FLOW HAS BEEN COMPLETE.
                        //tempWebserver.Stop();
                        break;
                    case "exp":
                        if (OAuthClient.checkJWT())
                            Print($"[Info]: Token expires in {APP_JWT_EXPIRY - GetUnixTimestamp()} seconds", 1);
                        else
                            Print($"[Info]: You do not have a token yet, or it is expired.", 1);
                        break;
                    case "resetenv":
                        Print($"[Shimamura]: WARNING This will reset all values in your .env file. Are you sure you wish to proceed? Y/N", 2);
                        while(true) {
                            var _confirm = Console.ReadLine().ToLower();

                            if (_confirm == "y" || _confirm == "yes") { write(true); Print($"[Shimamura]: .env has been reset to defaults.", 1); break; }
                            else if (_confirm == "n" || _confirm == "no") { Print($"[Shimamura]: Cancelled reset.", 1); break; }
                            else break;
                        }
                        break;
                    case "logging":
                        LOGGING_ENABLED = !LOGGING_ENABLED;
                        updateKey("LOGGING", LOGGING_ENABLED.ToString());
                        if(LOGGING_ENABLED) Print($"[Logger]: logging is now enabled", 1); else Print($"[Logger]: logging is now disabled", 1);
                        break;
                    case "help":
                        Print($"start - Starts the bot", 1);
                        Print($"stop - Stops the bot", 1);
                        Print($"exp - Shows how many seconds are left until your token expires", 1);
                        Print($"logging - Toggle logging on/off", 1);
                        Print($"listen - Starts the HTTPServer (deprecate)", 1);
                        Print($"stoplisten - Stops the HTTPServer (use this if the temporary Webserver doesn't shutdown for some reason)", 1);
                        Print($"resetenv - Resets the .env file to all default values (you will have to fill them all out again!)", 1);
                        Print($"exit or quit - Exits the program gracefully", 1);
                        Print($"say [msg]", 1);
                        Print($"whisper [usr][msg]", 1);

                        Print($"===== Modules ====", 1);

                        Print($"overunder - not finished", 1);
                        Print($"rage - vibrate?", 1);
                        Print($"eyes - smol eyes", 1);
                        Print($"tits - expose breasts", 1);
                        Print($"trip - trippy", 1);
                        Print($"cum  - liquid overhead", 1);
                        break;
                    default:
                        Print("[UserInputInvalid]: type help for list of commands", 1);
                        break;
                }
            }
        }

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

        private static async void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            await Task.Delay(100);
            Console.WriteLine("Exiting process...");

            //var stopped = await Task.Run(() => WaitForClosure());
            //ExitEvent.Set();
            MainLoop.ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Canceling process...");
            e.Cancel = true;
            MainLoop.ExitEvent.Set();
        }
    }
}
