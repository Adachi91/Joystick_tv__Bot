using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace ShimamuraBot
{
    class Program
    {
        #region apolloToken class import
        private token token = new token();
        public static string apolloSecret = token.secret2;
        public static string BotUUID = token.UUID;
        #endregion


        //private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        //public static CancellationTokenSource ShutdownToken = new CancellationTokenSource();

        //private static client wssClient = new client("wxx://FQDN/APIEndPoint?Token=Token", "shimamura", BotUUID, apolloSecret, "adachi91");

        //TODO: This needs a cfg file for UUIDs / port / etc
        public static int LoopbackPort = 8087;
        private void Print(string msg, int lvl) => events.Print(msg, lvl);

        //TODO: Refactor this entire fucking piece of shit
        public class MainThread
        {
            public readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
            public CancellationTokenSource isExiting = new CancellationTokenSource();
            private bool Started { get; set; } = false;

            public bool Running() { return Started; }
            public void Run() { Started = true; }
            public void Stop() { Started = false; }


            //I'm actually going to refactor this entire section it's going to call to MainLoop.acecssor/method

            public static Dictionary<string, long> FiveMillionTimers = new Dictionary<string, long>();
            //Dr. Evil air qouatation marks
            //Timer mytimer = new Timer(myTimeTicker);

            public MainThread()
            {
                //MainLoop.Start();
                //Remember to Thread.join() Jackass. 2024
            }

            //mainloop shit below this line turn back you don't want to die by reading what's below.
            private static HTTPServer server;
            public void Start() { MainLoop.Start(); }
            public void Touchy() { server.Stop(); }

            private Thread MainLoop = new Thread((object obj) => { //DOES THIS SHIT EVEN EXECUTE?!?!?
                //There are a lot of off-side threads running taskes such as tcp connections
                //it's pretty frustrating but there is very little "off the shelf" event connection to HTTPListener and I'm assuming HTTPClient,
                //though I do imagine HTTPClient will have SOME events to hook into and monitor, why is HTTPListener a little bitch? idk.

                //Manage, Monitor, Handle different aspects of the program while waiting for user input

                FiveMillionTimers.Add("Apples", 023985723985);

                /*
                 * init -> Thread.MainLoop(obj).Start() -> OAuth instance(ML) -> HTTPServer start(ML) -> Code populated -> HTTPServer GetToken(ML) ->
                 * > TokenManager(ML)
                 * OH that's what client does I think.
                 */

                server = new HTTPServer(oAuth);
                server.Start();
                //events.Print($"Server should have started thingy {asdf}", 0);

                /*if(((ct - cl) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 100) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 20) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0) //GOOOOOOOOOOOOOOOOOO

                if(((11 - 10) % 10) >= 0)*/ //GOOOOOOOOOOOOOOOOOO

                //shutdownreceive

            });
        }

        private static events.OAuthClient oAuth = new events.OAuthClient(token.baseAPIURI, token.clientId, token.clientSecret, @"https://127.0.0.1:8087/auth", token.Basic, token.customHeader, "bot");
        private static HTTPServer server = new HTTPServer(oAuth);
        private static VNyan vCat = new VNyan();
        public static MainThread MainLoop = new MainThread();
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;


            if (!MainLoop.Running()) MainLoop.Run();

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

            while (MainLoop.Running()) {
                Console.Write(">");
                string input = Console.ReadLine();

                switch (input.ToLower())
                {
                    case "exit" or "quit" or "stop":
                        //running = false;
                        MainLoop.Stop();
                        break;
                    case "start" or "run":
                        events.Print($"The circle is complete bitch, {oAuth.code.Substring(4, 10)}", 0);
                        //check if we have a valid token or refreshable token, do complete OAuth flow.
                        break;
                    case "config":
                        events.Print("Idk", 4);
                        //TODO: settings
                        break;
                    case "fun":
                        //foreach (var fuckyou in MainThread.FiveMillionTimers)
                        //Console.WriteLine($"fuckyou.key {fuckyou.Key} and fuckyou.value {fuckyou.Value}");
                        MainLoop.Start();
                        //Console.WriteLine($"Result of Millionsooftimerthing is {MainThread.FiveMillionTimers}");
                        //TODO: Setup things like YT, soundcloud, vemo video play commands, and other stuff
                        break;
                    case "sendit" or "oauth":
                        //oAuth.OpenbRowser(8087);
                        server.openBrowser(oAuth.fullURI);
                        break;
                    case "listen":
                        //server = new HTTPServer(oAuth);
                        server.Start();
                        ///events.Print($"Server should have started thingy {asdf}", 0);
                        ////server.StartAsync(oAuth);
                        break;
                    case "stoplisten":
                        //MainLoop.Touchy(); //why are you like this
                        server.Stop();
                        break;
                    case "fuckmylife":
                        //events.OAuthClient.VerifyPortAccessibility(LoopbackPort);
                        vCat.Redeem("Testa");
                        break;
                    case "vcat":
                        vCat.Redeem("meow");
                        break;
                    case "help":
                        events.Print($"help - IT DISPLAYS THIS FUCKING MESSAGE", 1);
                        events.Print($"listen - STARTS LISTENING ON THE TEMPORARY FUCKING SERVER OF HTTPLISTENER", 1);
                        events.Print($"stoplisten - STOP THE STUPID FUCKING HTTPLISTENER", 1);   
                        events.Print($"fun - IDK MASTURBATE?", 1);
                        events.Print($"quit/exit/something - IT EXPLODES", 1);
                        break;
                    default:
                        events.Print("type help", 1);
                        break;
                }

                //Console.Write("> ");
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
