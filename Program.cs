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

        public static int LoopbackPort = 8087;
        private void Print(string msg, int lvl) => events.Print(msg, lvl);
        public class MainThread //we don't ask why I do shit like this, I just accep it.
        {
            public readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
            public CancellationTokenSource isExiting = new CancellationTokenSource();
            private bool Started { get; set; }

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
            }

            //mainloop shit below this line turn back you don't want to die by reading what's below.
            private static TempServer server;
            public void Start() { MainLoop.Start(); }
            public void Touchy() { server.Stop(); }

            private Thread MainLoop = new Thread(async () => {
                //There are a lot of off-side threads running taskes such as tcp connections
                //it's pretty frustrating but there is very little "off the shelf" event connection to HTTPListener and I'm assuming HTTPClient,
                //though I do imagine HTTPClient will have SOME events to hook into and monitor, why is HTTPListener a little bitch? idk.

                //Manage, Monitor, Handle different aspects of the program while waiting for user input

                //I'm counting on you gohan, JK he's a little shit.

                FiveMillionTimers.Add("Apples", 023985723985);

                server = new TempServer(oAuth);
                var asdf = await server.Start();
                events.Print($"Server should have started thingy {asdf}", 0);

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

        private static TempServer server;
        private static events.OAuthClient oAuth = new events.OAuthClient(token.baseAPIURI, token.clientId, token.clientSecret, @"https://127.0.0.1:8087/auth", "bot");
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

            //Console.Write("> ");

            /*Thread meow = new Thread(() => {
                while (mainThread.thread2Running())
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
                    long unixTimestampMilliseconds = dateTimeOffset.ToUnixTimeMilliseconds();
                    Console.WriteLine($"Test {unixTimestampMilliseconds}");

                    Thread.Sleep(420);
                }
            });
            meow.Start();*/

            Console.WriteLine("Type help for commands, or start to start the applications.exe");

            while (MainLoop.Running()) {
                string input = Console.ReadLine();

                switch (input)
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
                        //events stateful = new events(token.username, BotUUID, "", "");
                        oAuth.OpenbRowser(8087);
                        break;
                    case "listen":
                        server = new TempServer(oAuth);
                        //var asdf = await server.Start();
                        //events.Print($"Server should have started thingy {asdf}", 0);
                        ////server.StartAsync(oAuth);
                        break;
                    case "stoplisten":
                        MainLoop.Touchy();
                        //server.Stop();
                        break;
                    case "fuckmylife":
                        //events.OAuthClient.VerifyPortAccessibility(LoopbackPort);
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

            return;

            /*var socket = ConstructCable().Result;
            while(socket._connected)
            {
                var input = Console.ReadLine();
                switch(input.ToLower())
                {
                    case "exit":
                        Task.Run(() => wssClient.Unsubscribe("disconnect", false)).Wait();
                        Console.WriteLine("|_./");
                        socket.Disconnect().Wait();
                        break;
                    case "sendit":
                        Task.Run(() => wssClient.Subscribe("connect", true));
                        break;
                }
            }*/
        }

        /*static async Task<client> ConstructCable()
        {
            await wssClient.Connect();
            Console.WriteLine("Main Thread: Connection: {0}", wssClient._connected);
            Task.Run(() => wssClient.Listen());
            return wssClient;
        }*/

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
