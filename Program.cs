using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;


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

        private const string subscription_type = "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"ApplicationChannel\\\"}\"}";
        private const string subscription_channel = "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"SystemEventChannel\\\",\\\"user_id\\\":\\\"adachi91\\\"}\"}";

        //private static client wssClient = new client(Joystick, "actioncable-v1-json", true);
        private static client wssClient = new client(Joystick, "shimamura", BotUUID, apolloSecret, "adachi91");

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;


            Console.WriteLine("|=======================|");
            Console.WriteLine("|    WEBSOCKET CLIENT   |");
            Console.WriteLine("|=======================|");
            Console.WriteLine();



            Console.WriteLine("====================================");
            Console.WriteLine("              STARTING              ");
            Console.WriteLine("====================================");

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
