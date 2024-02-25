using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
//using System.Threading.Channels;
using System.Collections.Generic;
using System.Linq;
//using System.Security.Cryptography.X509Certificates;
using System.Text;
//using System.Runtime.CompilerServices;
//using System.Reflection.Metadata;
using System.Timers;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ShimamuraBot
{
    class Program
    {
        //if you decode it let me know -adachi
        //public const string EASTER_EGG = "GAJ9MDCDIDEAHDTC9D9DEAADTCEAXCHDLAGDEAPC9DFDXCVCWCHDQAJ9HDTC9D9DEAADTCEASBLAADEAUCCDFDVCXCJDTCBDEAHDCDBDXCVCWCHDQAJ9QCIDHDEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCEABDCDKDQAJ9SBLAADEAWCCD9DSCXCBDVCEAIDDDEAPCEA9DXCVCWCHDQAJ9RCWCPCGDXCBDVCEACDIDHDEAHDWCTCEASCPCFDZCBDTCGDGDEAXCBDGDXCSCTCQAJ9RCPCIDGDTCEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCSAJ9GA";
        public static int LoopbackPort = 8087;

        public static string HOST = null;
        public static string CLIENT_ID = null;
        public static string CLIENT_SECRET = null;
        public static string WSS_HOST = null;
        public static string WSS_GATEWAY;
        public static string ACCESS_TOKEN; // B64 token
        public static object GATEWAY_IDENTIFIER;
        public static string APP_JWT; //bearer interact outside of message/events
        public static long APP_JWT_EXPIRY;
        public static string APP_JWT_REFRESH;
        public static string ENVIRONMENT_PATH;
        public static string CHANNELGUID = null;

        public const string HISTORY_PATH = @"shimamura.log";
        public static bool LOGGING_ENABLED = false;

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
            public void Touchy() { tempWebserver.Stop(); }

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
        private static HTTPServer tempWebserver;
        private static VNyan vCat = new VNyan();
        private static MainThread MainLoop = new MainThread();

        //only used for Module.Gamehandler testing
        private enum RandomNames {
            John,
            Jim,
            Mike,
            Paul,
            Roger,
            Jack,
            Chris,
            Jesus,
            Pickle,
            Slimeywiley,
            Jackass,
            Dopeshit,
            TAsskmunch,
            TacoBell,
            Jason,
            Razhal,
            Pain,
            Tickles,
            KittnLuvr44
        }

        private enum RandomPrizes {
            Tickle,
            Duckie,
            Fuck,
            Throw,
            Yeet
        }
        private static async Task Redeemer(string username, string prize)
        {
            var f = await isEligible(username, prize);
            if (f)
                Print($"[Redeemer]: {username} Redeeming {prize}", 0);
            else
                Print($"[Redeemer]: {username} Could not redeem {prize}, no points", 0);
        }
        //end test purge

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

            try { //I really over do stuff. like is this made for a 3 year old to run?
                envManager.load();
            } catch (Exception ex) {
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

            tempWebserver = new HTTPServer(oAuth);
            WebsocketClient wss = new WebsocketClient();

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
                var msg = "";
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
                else if (input.ToLower().StartsWith("lilpeep"))
                {
                    tits = input.Split(" ", 3, StringSplitOptions.RemoveEmptyEntries);
                    input = tits[0];
                    user = tits[1];
                    msg = tits[2];
                }
                else
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
                }

                switch (input.ToLower()) {
                    case "":
                        Print("", 0);
                        break;
                    case "test":
                        bool ttt;
                        for(int i = 0; i < 20; i++)
                        {
                            var player = Enum.GetNames(typeof(RandomNames))[new Random().Next(Enum.GetNames(typeof(RandomNames)).Length)];
                            var prizer = Enum.GetNames(typeof(RandomPrizes))[new Random().Next(Enum.GetNames(typeof(RandomPrizes)).Length)];
                            //_ = UpdateRewards(player, prizer, 1);
                            //ttt = await isEligible(player, prizer);
                            _ = Redeemer(player, prizer);
                        }
                        Print($"[System]: Dones", 0);
                        break;
                    case "overunder":
                        //start a new game of over/under
                        Modules.OverUnder Game = new Modules.OverUnder("tits");
                        break;
                    case "whisper":
                        _ = wss.sendMessage("send_whisper", new string[] { tits[2], tits[1], "" });
                        break;
                    case "say":
                        _ = wss.sendMessage("send_message", new string[] { tits[1], "", "" });
                        break;
                    case "mute":
                        int msgid;
                        string[] mutemsg;
                        //string mutemsgid;
                        try {
                            msgid = int.Parse(tits[2]);
                        } catch (Exception e) {
                            throw new BotException("System", $"Could not parse to int", e);
                        }
                        mutemsg = wss.getMessage(msgid);
                            _ = wss.sendMessage("mute_user", new string[] { "", mutemsg[0], mutemsg[1] });
                        break;
                    case "exit" or "quit":
                        //TODO: check if connected, if connected await stop(), then close.
                        return;//MainLoop.Stop();
                        break;
                    case "start" or "run": //TOSTAY
                        if (OAuthClient.checkJWT())
                            if(!wss.Open())
                                _ = wss.Connect();
                        else {
                            var _oauthComplete = await tempWebserver.Start();
                            if (_oauthComplete)
                                if(!wss.Open())
                                    _ = wss.Connect();
                        }
                        break;
                    case "stop":
                        if (wss.Open()) {
                            Print($"[Shimamura]: Stopping bot...", 1);
                            _ = wss.Close(-1);
                        } else
                            Print($"[Shimamura]: Bot is not currently active!", 2);
                        break;
                    case "listen"://PRUNE AFTER FLOW HAS BEEN COMPLETE.
                        _ = tempWebserver.Start();
                        break;
                    case "stoplisten"://PRUNE  AFTER FLOW HAS BEEN COMPLETE.
                        tempWebserver.Stop();
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
                        break;
                    default:
                        Print("[UserInputInvalid]: type help for list of commands", 1);
                        break;
                }
            }
        }

        private static async Task<bool> WaitForClosures()
        {//to even start I need to have a pool of resources to check list down to make sure are closed and disposed or at least gracefully closed.
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
