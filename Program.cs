using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ShimamuraBot
{
    class Program
    {
        #region apolloToken class import
        private token token = new token();
        public static string apolloSecret = token.secret2;
        public static string BotUUID = token.UUID;
        #endregion

        //private static Uri testy = new Uri("wss://socketsbay.com/wss/v2/1/demo/");
        private static Uri Joystick = new Uri("wss://joystick.tv/cable?token=" + apolloSecret);

        //private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        //public static CancellationTokenSource ShutdownToken = new CancellationTokenSource();

        //private static client wssClient = new client(Joystick, "shimamura", BotUUID, apolloSecret, "adachi91");
        //private bool mainThread = true;

        public static int LoopbackPort = 8087;

        public class mainThread //we don't ask why I do shit like this, I just accep it.
        {
            private static bool isRunning { get; set; }
            public static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
            public static CancellationTokenSource isExiting = new CancellationTokenSource();
            private static bool MainLoopStarted { get; set; }

            //I'm actually going to refactor this entire section it's going to call to MainLoop.acecssor/method

            private static Dictionary<string, long> FiveMillionTimers = new Dictionary<string, long>();
            //Dr. Evil air qouatation marks
            private static Thread MainLoop = new Thread(() => {
                //There are a lot of off-side threads running taskes such as tcp connections
                //it's pretty frustrating but there is very little "off the shelf" event connection to HTTPListener and I'm assuming HTTPClient,
                //though I do imagine HTTPClient will have SOME events to hook into and monitor, why is HTTPListener a little bitch? idk.

                //Manage, Monitor, Handle different aspects of the program while waiting for user input

                //I'm counting on you gohan, JK he's a little shit.
            });


            /// <summary>
            /// Get state of the Main Thread
            /// </summary>
            /// <returns>bool</returns>
            public static bool Running() { return isRunning; }
            /// <summary>
            /// Start Main Thread
            /// </summary>
            public static void Start() { isRunning = true; }
            /// <summary>
            /// Stop Main Thread
            /// </summary>
            public static void Stop() {  isRunning = false; }

            public static bool thread2Running() { return MainLoopStarted; }
            public static void startshit() { MainLoopStarted = true; }
            public static void stopshit() { MainLoopStarted = false; }
        }

       // private static Dictionary<int, string> consoleBuffer = new Dictionary<int, string>(100);
        private static List<string> consoleBuffer = new List<string>();
        private static int BufferSizeMax = 100;
        private static TempServer server;
        private static events.OAuthClient oAuth = new events.OAuthClient(token.baseAPIURI, token.clientId, token.clientSecret, @"https://127.0.0.1:8087/auth", "bot");




        /// <summary>
        /// Renders the output display, YES I really spent all morning writing this and I feel horrible about it.
        /// </summary>
        /// <param name="input">the user input</param>
        /// <param name="initRender">only use for initial rendering</param>
        private static void HandleBuffer(string input, string headerBorder, bool initRender = false)
        {
            int rows = Console.WindowHeight - 4;
            //int conswidth = Console.WindowWidth;
            Console.Clear();
            ConsoleColor origColor = Console.ForegroundColor;

            //render header
            Console.SetCursorPosition(0, 0);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(headerBorder);
            Console.Write($"=   Shimamura Bot {Assembly.GetExecutingAssembly().GetName().Version}  ##  Status: ");
            Console.ForegroundColor = mainThread.Running() == true ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write("{0}", mainThread.Running() == true ? "Online" : "Offline");
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(headerBorder);
            Console.ForegroundColor = origColor;


            /*
             * 
             * I gave up on colorful console it sux, and I don't feel like really making a winform and my love for consoles tells me it shall be this, idk maybe make a GUI wrapper and accept parameters from it if anyone ever wants this pile of shit
             */ 

            if (initRender) return;

            //shift buffer
            if (consoleBuffer.Count >= BufferSizeMax)
                consoleBuffer.RemoveAt(0);
            consoleBuffer.Add(input);

            //truncate what can be seen by how large window is add a resize monitor? idk
            if (consoleBuffer.Count > rows)
            {
                int trunc = consoleBuffer.Count - rows;
                for (int i = trunc; i <= consoleBuffer.Count - 1; i++)
                {
                    Console.WriteLine($"$ {consoleBuffer[i]}");
                }
            }
            else {
                foreach (var msg in consoleBuffer)
                    Console.WriteLine($"$ {msg}");
            }
        }

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            if (!mainThread.Running()) mainThread.Start();
            mainThread.startshit();

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
            Console.ForegroundColor = ConsoleColor.White;

            Console.Write("> ");

            Thread meow = new Thread(() => {
                while (mainThread.thread2Running())
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.UtcNow;
                    long unixTimestampMilliseconds = dateTimeOffset.ToUnixTimeMilliseconds();
                    Console.WriteLine($"Test {unixTimestampMilliseconds}");

                    Thread.Sleep(420);
                }
            });
            meow.Start();
            //dual threaded or single threaded for main loop? I don't fucking know the console input is basically locked waiting for user input
            //so it can't do background taskes so I think I need a true mainloop

            while (mainThread.Running()) {
                string input = Console.ReadLine();

                switch (input)
                {
                    case "exit" or "quit" or "stop":
                        //running = false;
                        mainThread.Stop();
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
                        mainThread.stopshit();
                        //TODO: Setup things like YT, soundcloud, vemo video play commands, and other stuff
                        break;
                    case "sendit" or "oauth":
                        //events stateful = new events(token.username, BotUUID, "", "");
                        oAuth.OpenbRowser(8087);
                        break;
                    case "listen":
                        server = new TempServer(oAuth);
                        //server.StartAsync(oAuth);
                        break;
                    case "stoplisten":
                        server.Stop();
                        break;
                    case "fuckmylife":
                        events.OAuthClient.VerifyPortAccessibility(LoopbackPort);
                        break;
                    default:
                        Console.WriteLine("type help");
                        break;
                }

                Console.Write("> ");
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
            mainThread.ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Canceling process...");
            e.Cancel = true;
            mainThread.ExitEvent.Set();
        }
    }
}
