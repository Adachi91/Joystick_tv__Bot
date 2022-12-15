using System;
using System.IO;
using System.Net.WebSockets;
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

        private static Uri testy = new Uri("wss://socketsbay.com/wss/v2/1/demo/");
        private Log logr = new Log();

        static void Main(string[] args)
        {
            var exitEvent = new ManualResetEvent(false);

            using (var client = new WebsocketClient(testy))
            {
                client.ReconnectTimeout = TimeSpan.FromSeconds(30);
                client.ReconnectionHappened.Subscribe(info =>
                    Log.Information($"Reconnection happened, type: {info.Type}"));

                client.MessageReceived.Subscribe(msg => Log.Information($"Message received: {msg}"));
                client.Start();

                Task.Run(() => client.Send("{ \"message\": \"csharp websocket.client Adi\" }"));

                exitEvent.WaitOne();
            }

            Console.WriteLine("Hello World!");
        }
    }
}
