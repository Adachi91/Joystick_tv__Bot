using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.ComponentModel;

namespace ShimamuraBot
{
    /// <summary>
    ///  VNyan Websocket Extension to allow chat to tip to interact with VNyan (e.g. Throw items, Bonk, Change Pose, etc... of the VTuber)
    ///  endpoint:: ws://localhost:8000/vnyan
    /// </summary>
    internal class VNyan : IDisposable
    {
        private string name = "vNyan";

        public VNyan() { }
        public void Dispose() { }

        public void Redeem(string type) {
            /* TODO: Check if socket can be opened before procededing */
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
                    SendTovNyan(type);
                    break;
            }
        }


        /// <summary>
        ///  Send message to vNyan WebSocket
        /// </summary>
        /// <param name="msg"><see cref="string"/> Plain string</param>
        private async Task SendTovNyan(string msg) { // So eh, yeah. I figured out why it crashes sometimes. Don't use `Async Void`s.
            using (ClientWebSocket vNyan = new ClientWebSocket()) {
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

                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                Print(this.name, $"[vNyan]: Attempting to send {msg} to vNyan", PrintSeverity.Debug);

                try {
                    //Use IPv4 localhost instead of 'localhost' because it will try and route to IPv6 and bounce around causing up to 1 second latency.
                    await vNyan.ConnectAsync(new Uri("ws://127.0.0.1:8000/vnyan"), default);
                    await vNyan.SendAsync(buffer, WebSocketMessageType.Text, true, default);
                } catch (Exception ex) {
                    new BotException(this.name, $"Error when trying to send vNyan WebSocket message.", ex);
                }
            }
        }
    }
}
