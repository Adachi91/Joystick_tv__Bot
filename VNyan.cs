using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;

namespace ShimamuraBot
{
    /// <summary>
    /// VNyan Websocket Extension to allow chat to tip to interact with VNyan (e.g. Throw items, Bonk, Change Pose, etc... of the VTuber)
    /// endpoint:: wss://localhost:8000/vnyan
    /// 
    /// SOME FUCKING DOCUMENTATION WOULD BE GREAT, THANKS.
    /// </summary>
    internal class VNyan
    {
        //private CancellationTokenSource cts = new CancellationTokenSource();
        //private CancellationToken cancelRequest = new CancellationToken();

        public void Redeem(string type) { //TODO: add a check to make sure ws connectivity is available for vNyan otherwise return a user friendly message
            switch(type) {
                case "yeet":
                    SendTovNyan("Test");
                    break;
                case "duck":
                    SendTovNyan("duck");
                    break;
                case "meow":
                    SendTovNyan("meow");
                    break;
                case "tta":
                    SendTovNyan("tta");
                    break;
                default:
                    break;
            }
        }
        public void stopvNyan()
        {
            //cts.Cancel();
        }

        private async void SendTovNyan(string msg)
        {
            using(ClientWebSocket vNyan = new ClientWebSocket())
            {
                //vNyan.Options.AddSubProtocol("wss");

                //This must be the wrong struct and it's just responding by throwing at a duck at me to tell me to bugfix. I wonder if it's returning anything
                //var payload = new
                //{ // so I have no idea the structure apparently.
                //title = msg,
                //Data = msg
                //};
                //holy.
                //fucking.
                //shit.
                //what a shit, you know what. I don't care anymore. 4 hours later


                /*
                 * 
                 * 
                 *                              this
                 * 
                 * 
                 *                              is
                 * 
                 * 
                 *                              why
                 * 
                 * 
                 *                              documentation is important
                 * 
                 * 
                 *                              not your stupid fucking discord server.
                 *                              full stop
                 *                              
                 *                              https://discord.com/channels/714814460010823690/1041200204742934578/1041261257078079578
                 */

                //string jsonPayload = JsonSerializer.Serialize(payload);
                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                Print($"[vNyan]: Attempting to send {msg} to vNyan", 0);

                try {
                    //Use IPv4 localhost instead of 'localhost' because it will try and route to IPv6 and bounce around causing up to 1 second latency.
                    await vNyan.ConnectAsync(new Uri("ws://127.0.0.1:8000/vnyan"), default);
                    _ = vNyan.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, default);
                } catch (Exception ex) {
                    Print($"[vNyan]: {ex}", 3);
                }

                Print($"[vNyan]: Disposing Websocket Client.", 0);
            }
        }
    }
}
