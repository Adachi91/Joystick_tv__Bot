using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection.Metadata;

namespace ShimamuraBot
{
    class Program
    {
        //if you decode it let me know -adachi
        //public const string EASTER_EGG = "GAJ9MDCDIDEAHDTC9D9DEAADTCEAXCHDLAGDEAPC9DFDXCVCWCHDQAJ9HDTC9D9DEAADTCEASBLAADEAUCCDFDVCXCJDTCBDEAHDCDBDXCVCWCHDQAJ9QCIDHDEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCEABDCDKDQAJ9SBLAADEAWCCD9DSCXCBDVCEAIDDDEAPCEA9DXCVCWCHDQAJ9RCWCPCGDXCBDVCEACDIDHDEAHDWCTCEASCPCFDZCBDTCGDGDEAXCBDGDXCSCTCQAJ9RCPCIDGDTCEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCSAJ9GA";
        public static int LoopbackPort = 8087;

        public static string HOST;
        public static string CLIENT_ID;
        public static string CLIENT_SECRET;
        public static string WSS_HOST;
        public static string WSS_GATEWAY;
        public static string ACCESS_TOKEN; // B64 token
        public static object GATEWAY_IDENTIFIER;
        public static string APP_JWT; //bearer interact outside of message/events
        public static long APP_JWT_EXPIRY;
        public static string APP_JWT_REFRESH;
        public static string ENVIRONMENT_PATH;

        public const string HISTORY_PATH = @"shimamura.log";
        public static bool LOGGING_ENABLED;
        

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
            public void Touchy() { server.Stop(); }

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

                //server = new HTTPServer(oAuth);
                //server.Start();
                //events.Print($"Server should have started thingy {asdf}", 0);

                /*if(((ct - cl) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 100) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 20) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0)*/ //GOOOOOOOOOOOOOOOOOO
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
        private static HTTPServer server;
        private static VNyan vCat = new VNyan();
        public static MainThread MainLoop = new MainThread();
        static void Main(string[] args)
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

            envManager.load();

            oAuth = new OAuthClient(
                HOST,
                CLIENT_ID,
                CLIENT_SECRET,
                "/api/oauth/authorize",
                "/api/oauth/token",
                $"https://127.0.0.1:{LoopbackPort}/auth",
                "bot"
            );

            server = new HTTPServer(oAuth); //temporary HTTPListener for authorization code
            WebsocketClient wss = new WebsocketClient();

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
            var x = true;
            while (x) {
                Console.Write(">");
                string input = Console.ReadLine();

                switch (input.ToLower())
                {
                    case "":
                        break;
                    case "test":
                        MainLoop.isExiting.Cancel();
                        break;
                    case "fuck":
                        //wss.bakeacake(new { });
                        wss.sendMessage("unsubscribe");
                        wss.Close();
                        break;
                    case "exit" or "quit":
                        x = false;//MainLoop.Stop();
                        break;
                    case "start" or "run": //TOSTAY
                        if(OAuthClient.checkJWT()) {
                            WSS_GATEWAY = $"{WSS_HOST}?token={ACCESS_TOKEN}";
                            wss.Connect();
                            Task.Run(() => wss.Listen(wss.ctx));

                            wss.sendMessage("subscribe");
                        } else {
                            server.Start();
                            oAuth.State = OAuthClient.Generatestate();
                            server.openBrowser(oAuth.Auth_URI.ToString() + $"?client_id={CLIENT_ID}&scope=bot&state={oAuth.State}");
                        }
                        break;
                    case "stop":
                        Print($"[MainThread]: Attempting to close socket", 0);
                            wss.Close();
                        break;
                    case "config"://ehhhhhhhh drake this one
                        var t = Task.Run(() => {
                            oAuth.callmewhateverlater(1);
                        });
                        Print("Idk - dumped oauth code to oauth2.txt", 4);
                        //TODO: settings
                        break;
                    case "sendit" or "oauth"://PRUNE  AFTER FLOW HAS BEEN COMPLETE.
                        oAuth.State = OAuthClient.Generatestate();
                        server.openBrowser(oAuth.Auth_URI.ToString() + $"?client_id={CLIENT_ID}&scope=bot&state={oAuth.State}");
                        break;
                    case "listen"://PRUNE AFTER FLOW HAS BEEN COMPLETE.
                        server.Start();
                        break;
                    case "stoplisten"://PRUNE  AFTER FLOW HAS BEEN COMPLETE.
                        server.Stop();
                        break;
                    case "vcat"://PRUNE
                        vCat.Redeem("meow");
                        break;
                    case "exp":
                        Print($"[Info]: Token expires in {APP_JWT_EXPIRY - GetUnixTimestamp()} seconds", 1);
                        break;
                    case "help":
                        Print($"start - starts the bot", 1);
                        Print($"stop - stops the bot", 1);
                        Print($"exp - shows how many seconds are left until your token expires", 1);
                        Print($"listen - starts the HTTPServer (deprecate)", 1);
                        Print($"stoplisten - stops the HTTPServer (depcreate)", 1);   
                        Print($"exit or quit - exits the program gracefully", 1);
                        break;
                    default:
                        Print("[UserInputInvalid]: type help for list of commands", 1);
                        break;
                }
            }
                
                //I was euuuuuuhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh (-drake) testing out a switch feature I didn't know existed.
                /*var resultText = txt switch
                {
                    "20" or "22" => "I'm literally regarded.",
                    "exit" => "ZERO ZERO TWO BEST GIRL",
                    _ => "Shit"
                };*/
        }

        private static async Task<bool> WaitForClosures()
        {//to even start I need to have a pool of resources to check list down to make sure are closed and disposed or at least gracefully closed.

            return true;
        }


        /*
         * Token management needs to happen before closure
         * Also eh, idk draw an owl
         */

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
