using System;
using System.IO;
using System.Collections;
using System.Data;
using System.Runtime;
using System.Windows;
//using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Websocket.Client;
using Serilog;
using Serilog.Events;
//using Websocket;

namespace Joystick_tv__Bot
{
    class Program
    {
        #region apolloToken class import
        private token token = new token();
        public static string apolloSecret = token.secret;
        #endregion

        //private static Uri testy = new Uri("wss://socketsbay.com/wss/v2/1/demo/");
        private static Uri Joystick = new Uri("wss://joystick.tv/cable?token=" + apolloSecret);

        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
        //private Log logging = new LoggerConfiguration();
        public static TbsLoggerSink LoggerSink = new TbsLoggerSink();
        public static readonly Serilog.Core.Logger Log = new LoggerConfiguration()
                    .WriteTo.Sink(LoggerSink)
                    .CreateLogger();

        //{"command":"subscribe","identifier":\"{\"channel\":\"ApplicationChannel\"}"}
        private const string subscription_type = "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"ApplicationChannel\\\"}\"}";
        private const string subscription_channel = "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"SystemEventChannel\\\",\\\"user_id\\\":\\\"adachi91\\\"}\"}";
        //{"command":"subscribe","identifier":"{\"channel\":\"SystemEventChannel\",\"user_id\":\"adachi91\"}"}

        static void Main(string[] args)
        {
            InitLogging();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;


            Console.WriteLine("|=======================|");
            Console.WriteLine("|    WEBSOCKET CLIENT   |");
            Console.WriteLine("|=======================|");
            Console.WriteLine();

            

            Log.Debug("====================================");
            Log.Debug("              STARTING              ");
            Log.Debug("====================================");

            //var exitEvent = new ManualResetEvent(false);

            using (var client = new WebsocketClient(Joystick))
            {
                client.Name = "TestyMcTestFace";
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ReconnectionHappened.Subscribe(info =>
                    Log.Information($"Reconnection happened, type: {info.Type}"));
                client.DisconnectionHappened.Subscribe(type =>
                    Log.Warning($"Disconnection happened, type: {type}"));

                client.MessageReceived.Subscribe(msg => Log.Information($"Message received: {msg}"));
                //client.MessageReceived.Subscribe(msg => Console.WriteLine("{$0}", msg));
                client.Start();

                Task.Run(() => client.Send(subscription_type));

                ExitEvent.WaitOne();
            }

            Log.Debug("====================================");
            Log.Debug("              STOPPING              ");
            Log.Debug("====================================");
            //Log.CloseAndFlush();

            Console.WriteLine("Hello World!");
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

        private static void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            Log.Warning("Exiting process");
            ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Log.Warning("Canceling process");
            e.Cancel = true;
            ExitEvent.Set();
        }
    }
}
