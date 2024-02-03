﻿using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        public static int LoopbackPort = 8087;

        public static string HOST;
        public static string CLIENT_ID;
        public static string CLIENT_SECRET;
        public static string WSS_HOST;
        public static string WSS_GATEWAY;
        //public static string ACCESS_TOKEN; // I use APP_JWT identifier to find the JWT, expiry, and refresh tokens faster
        public static object GATEWAY_IDENTIFIER;
        public static string APP_JWT;
        public static long APP_JWT_EXPIRY;
        public static string APP_JWT_REFRESH;
        public static string ENVIRONMENT_PATH;

        public const string HISTORY_PATH = @"shimaura.log";
        public static bool LOGGING_ENABLED;
        

        #region MainLoopMultiThreading_TODO
        //private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        //public static CancellationTokenSource ShutdownToken = new CancellationTokenSource();

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

            envManager.load(ENVIRONMENT_PATH);

            //Now we have info.

            //oAuth = new events.OAuthClient(HOST, CLIENT_ID, CLIENT_SECRET, $"https://127.0.0.1:{LoopbackPort}/auth", ACCESS_TOKEN, "X-JOYSTICK-STATE", "bot");
            oAuth = new OAuthClient(
                HOST,
                CLIENT_ID,
                CLIENT_SECRET,
                "/api/oauth/authorize",
                "/api/oauth/token",
                $"https://127.0.0.1:{LoopbackPort}/auth",
                "bot"
            );

            server = new HTTPServer(oAuth);
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

            ///events.Print($"[.env]: Host: {HOST}", 0);
            ///events.Print($"[.env]: ID: {CLIENT_ID}", 0);
            ///events.Print($"[.env]: Secret: {CLIENT_SECRET}", 0);
            ///events.Print($"[.env]: WSS: {WSS_HOST}", 0);
            ///events.Print($"[.env]: Token: {ACCESS_TOKEN}", 0);
            ///events.Print($"[.env]: Gateway: {GATEWAY_IDENTIFIER}", 0);
            ///events.Print($"[.env]: Refresh: {APP_JWT_REFRESH}", 0);
            events.Print($"[environment]: Successfully loaded environment file.", 1);

            while (MainLoop.Running()) {
                Console.Write(">");
                string input = Console.ReadLine();

                switch (input.ToLower())
                {
                    case "":
                        break;
                    case "fuck":
                        //wss.bakeacake(new { });
                        Print($"ff", 0);
                        break;
                    case "exit" or "quit":
                        //running = false;
                        MainLoop.Stop();
                        break;
                    case "start" or "run": //TOSTAY
                        if(!string.IsNullOrEmpty(APP_JWT)) {
                            if(GetUnixTimestamp() - APP_JWT_EXPIRY <= 0) {
                                WSS_GATEWAY = $"{WSS_HOST}?token={APP_JWT}";
                                wss.Connect();
                                Task.Run(() => wss.Listen());
                                wss.sendMessage("subscribe");
                            } else {
                                server.Start();
                                oAuth.State = OAuthClient.Generatestate();
                                server.openBrowser(oAuth.Auth_URI.ToString() + $"?client_id={CLIENT_ID}&scope=bot&state={oAuth.State}");
                            }
                        } else {
                            //draw the rest of the fucking owl.
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
                        //File.WriteAllText("oAuth2.txt", oAuth.OAuthCode);
                        var t = Task.Run(() => {
                            oAuth.callmewhateverlater(1);
                        });
                        events.Print("Idk - dumped oauth code to oauth2.txt", 4);
                        //TODO: settings
                        break;
                    case "fun"://PRUNE?
                        //foreach (var fuckyou in MainThread.FiveMillionTimers)
                        //Console.WriteLine($"fuckyou.key {fuckyou.Key} and fuckyou.value {fuckyou.Value}");
                        MainLoop.Start();
                        //Console.WriteLine($"Result of Millionsooftimerthing is {MainThread.FiveMillionTimers}");
                        //TODO: Setup things like YT, soundcloud, vemo video play commands, and other stuff
                        break;
                    case "sendit" or "oauth"://PRUNE  AFTER FLOW HAS BEEN COMPLETE.
                        oAuth.State = OAuthClient.Generatestate(); // FIX THIS SHIT PELASE
                        server.openBrowser(oAuth.Auth_URI.ToString() + $"?client_id={CLIENT_ID}&scope=bot&state={oAuth.State}");
                        break;
                    case "listen"://PRUNE AFTER FLOW HAS BEEN COMPLETE.
                        //server = new HTTPServer(oAuth);
                        server.Start();
                        ///events.Print($"Server should have started thingy {asdf}", 0);
                        ////server.StartAsync(oAuth);
                        break;
                    case "stoplisten"://PRUNE  AFTER FLOW HAS BEEN COMPLETE.
                        //MainLoop.Touchy(); //why are you like this
                        server.Stop();
                        break;
                    case "vcat"://PRUNE
                        vCat.Redeem("meow");
                        break;
                    case "help"://Fix
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
