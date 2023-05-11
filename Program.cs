﻿using System;
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

        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);

        //private static client wssClient = new client(Joystick, "actioncable-v1-json", true);
        private static client wssClient = new client(Joystick, "shimamura", BotUUID, apolloSecret, "adachi91");

        private static bool running = true;
       // private static Dictionary<int, string> consoleBuffer = new Dictionary<int, string>(100);
        private static List<string> consoleBuffer = new List<string>();
        private static int BufferSizeMax = 100;
        private static TempServer server = new TempServer();
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
            Console.ForegroundColor = running == true ? ConsoleColor.Green : ConsoleColor.Red;
            Console.Write("{0}", running == true ? "Online" : "Offline");
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

            while (running) {
                string input = Console.ReadLine();

                switch(input)
                {
                    case "exit" or "quit" or "stop":
                        running = false;
                        break;
                    case "oauth":
                        //TODO: create class and test OAuth on playground.
                        break;
                    case "config":
                        //TODO: settings
                        break;
                    case "fun":
                        //TODO: Setup things like YT, soundcloud, vemo video play commands, and other stuff
                        break;
                    case "sendit":
                        events stateful = new events(token.username, BotUUID, "", "");
                        events.OAuthClient oAuth = new events.OAuthClient("", "", "","","bot");
                        oAuth.SendA();
                        break;
                    case "listen":
                        server.StartAsync();
                        break;
                    case "stoplisten":
                        server.Stop();
                        break;
                    default:
                        Console.WriteLine("type help");
                        break;
                }

                Console.Write("> ");
            }

            return;


            Console.WriteLine("|=======================|");
            Console.WriteLine("|    WEBSOCKET CLIENT   |");
            Console.WriteLine("|=======================|");
            Console.WriteLine();



            Console.WriteLine("====================================");
            Console.WriteLine("              ShimamuraBot          ");
            Console.WriteLine("              Version {0}           ", Assembly.GetExecutingAssembly().ImageRuntimeVersion);
            Console.WriteLine("====================================");
            Console.SetCursorPosition(0, Console.WindowHeight - 1);

            /************************************************************************************************************************
             * Gather around boys and girls while I try and learn OAuth flow. What? I like being behind the curve.
             ************************************************************************************************************************/

            Console.Write(">");
            while (running)
            {
                var txt = Console.ReadLine();

                switch(txt.ToLower())
                {
                    case "exit" or "pizza":
                        running = false;
                        break;
                }

                
                //I was euuuuuuhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh (-drake) testing out a switch feature I didn't know existed.
                /*var resultText = txt switch
                {
                    "20" or "22" => "I'm literally regarded.",
                    "exit" => "ZERO ZERO TWO BEST GIRL",
                    _ => "Shit"
                };*/
            }

            Console.WriteLine("Goodbye !");

            return;

            var socket = ConstructCable().Result;
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
            }
            
            Console.WriteLine("====================================");
            Console.WriteLine("              STOPPING              ");
            Console.WriteLine("====================================");
        }

        static async Task<client> ConstructCable()
        {
            await wssClient.Connect();
            Console.WriteLine("Main Thread: Connection: {0}", wssClient._connected);
            //await wssClient.Subscribe("connect");
            Task.Run(() => wssClient.Listen());
            return wssClient;
        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("Exiting process");
            ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Canceling process");
            e.Cancel = true;
            ExitEvent.Set();
        }
    }
}
