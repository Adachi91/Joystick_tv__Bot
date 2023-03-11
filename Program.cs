using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
//using Serilog;
//using Serilog.Events;


namespace Joystick_tv__Bot
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
        //private Log logging = new LoggerConfiguration();
        //public static TbsLoggerSink LoggerSink = new TbsLoggerSink();
        /*public static readonly Serilog.Core.Logger Log = new LoggerConfiguration()
                    .WriteTo.Sink(LoggerSink)
                    .CreateLogger();*/

        //{"command":"subscribe","identifier":\"{\"channel\":\"ApplicationChannel\"}"}
        private const string subscription_type = "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"ApplicationChannel\\\"}\"}";
        private const string subscription_channel = "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"SystemEventChannel\\\",\\\"user_id\\\":\\\"adachi91\\\"}\"}";
        //{"command":"subscribe","identifier":"{\"channel\":\"SystemEventChannel\",\"user_id\":\"adachi91\"}"}
        //private static WebsocketClient client = new WebsocketClient(Joystick);
        private static client wssClient = new client(Joystick, "actioncable-v1-json", true);
        static void Main(string[] args)
        {
            //InitLogging();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;


            Console.WriteLine("|=======================|");
            Console.WriteLine("|    WEBSOCKET CLIENT   |");
            Console.WriteLine("|=======================|");
            Console.WriteLine();



            Console.WriteLine("====================================");
            Console.WriteLine("              STARTING              ");
            Console.WriteLine("====================================");

            var exitEvent = new ManualResetEvent(false);

            //using (var client = new WebsocketClient(Joystick))
             //{
                //client.Name = "TestyMcTestFace";
                /*client.NativeClient.Options.AddSubProtocol("actioncable-v1-json");
                Websocket.Client.
                 client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                 client.ReconnectionHappened.Subscribe(info =>
                     Log.Information($"Reconnection happened, type: {info.Type}"));
                 client.DisconnectionHappened.Subscribe(type =>
                     Log.Warning($"Disconnection happened, type: {type}"));

                 client.MessageReceived.Subscribe(msg => Log.Information($"Message received: {msg}"), onCompleted => Cors(onCompleted));
                 //client.MessageReceived.Subscribe(msg => Console.WriteLine("{$0}", msg));
                 client.Start();*/

                 //Console.WriteLine("RAW: {0}", client.MessageReceived);

                 //ExitEvent.WaitOne();
             //}

            string[] clientOptions = { "actioncable-v1-json" };

            var socket = ConstructCable().Result;
            while(socket._connected)
            {
                var input = Console.ReadLine();
                if (input.ToLower() == "exit")
                {
                    socket.Disconnect().Wait();
                    break;
                }
                else
                {
                    //socket.SendMessage(input).Wait();
                }
            }
            /*using (WebSocket wssClient = new WebSocket(Joystick.ToString(), true, clientOptions))
            {
                wssClient.OnMessage += (sender, e) => Console.WriteLine("[Socket]: {0}", e.Data);
                wssClient.OnClose += (sender, e) => Console.WriteLine("[Socket]: Closure {0}", e.WasClean);


                wssClient.Connect();
                while (true)
                {
                    Console.Write("> ");
                    var msg = Console.ReadLine();

                    switch (msg)
                    {
                        case "exit":
                            Console.WriteLine("Exiting wss!");
                            wssClient.Close(CloseStatusCode.Away, "Leaving");
                            break;
                        case "send":
                            break;
                        case "headers":
                            Console.WriteLine("Headers: {0}", wssClient.hitAllTheWalls(0));
                            break;
                        case "proto":
                            Console.WriteLine("[Sys]: {0}", wssClient.hitAllTheWalls(3));
                            break;
                        case "wss":
                            Console.WriteLine("IsSecure: {0}", wssClient.hitAllTheWalls(2));
                            break;
                        case "host":
                            Console.WriteLine("Host: {0}", wssClient.hitAllTheWalls(1));
                            break;
                        case "nip":
                            Console.WriteLine("{0}", wssClient.hitAllTheWalls());
                            break;
                        default:
                            Console.WriteLine("Do something..");
                            break;

                    }
                }
            }*/

            Console.WriteLine("====================================");
            Console.WriteLine("              STOPPING              ");
            Console.WriteLine("====================================");
            //Log.CloseAndFlush();

            Console.WriteLine("Hello World!");
        }

        static async Task<client> ConstructCable()
        {
            await wssClient.Connect();
            Console.WriteLine("Main Thread: Connection: {0}", wssClient._connected);
            await wssClient.Subscribe("adachi91");
            Task.Run(() => wssClient.Listen());
            return wssClient;
        }

        private static void Cors(System.Exception a)
        {
            Console.WriteLine("[CORE]: {0}", a);
        }

        private static void InitLogging()
        {
            var executingDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            var logPath = Path.Combine(executingDir, "logs", "verbose.log");

            //file Catzoo = Directory.GetCurrentDirectory();

            /*Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
                .WriteTo.ColoredConsole(LogEventLevel.Verbose)
                .CreateLogger();*/
        }

        /*private static async Task StartSendingPing(IWebsocketClient client)
        {
            while (true)
            {
                await Task.Delay(1000);

                if (!client.IsRunning)
                    continue;

                client.Send("ping");
            }
        }*/

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
