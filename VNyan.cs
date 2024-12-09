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
    /// VNyan Websocket Extension to allow chat to tip to interact with VNyan (e.g. Throw items, Bonk, Change Pose, etc... of the VTuber)
    /// endpoint:: ws://localhost:8000/vnyan
    /// </summary>
    internal class VNyan
    {
        private string name = "vNyan";
        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken cancelRequest;

        private ClientWebSocket _ws_connection_port_socket_unix_reverse_uno;

        /// <summary>
        /// Dumstructor.
        /// </summary>
        public VNyan() {
            _ws_connection_port_socket_unix_reverse_uno = new ClientWebSocket();
            cancelRequest = cts.Token;
        }

        public void Stop() { //deprecated
            if (!cts.IsCancellationRequested || !cts.Token.CanBeCanceled || !cts.Token.IsCancellationRequested)
                cts.Cancel();
            else
                Print(this.name, $"A secondary cancellation request was sent to vNyan.", PrintSeverity.Debug);
        }

        /// <summary>
        /// Opens the vNyan websocket connection.
        /// </summary>
        /// <param name="Host">Optional - Defaults to vNyan default localhost:8000/vnyan</param>
        /// <returns>Boolean - Connection Status</returns>
        public async Task<bool> Start(string Host = "ws://127.0.0.1:8000/vnyan") => await _start(Host);

        private async Task<bool> _start(string Host) {
            try {
                await _ws_connection_port_socket_unix_reverse_uno.ConnectAsync(new Uri(Host), cancelRequest);
                if (_ws_connection_port_socket_unix_reverse_uno.State == WebSocketState.Open) return true; else return false;
            } catch (Exception ex) {
                new BotException("vNyan", "Could not open a websocket connection.\n", ex);
                return false;
            }
        }

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
                    SendTovNyan(type);
                    break;
            }
        }

        private async void SendTovNyan(string msg)
        {
            using(ClientWebSocket vNyan = new ClientWebSocket())
            {
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
                    _ = vNyan.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, default);
                } catch (Exception ex) {
                    new BotException(this.name, $"{ex}");
                    //Print(this.name, $"[vNyan]: {ex}", PrintSeverity.Error);
                }

                //Print($"[vNyan]: Disposing Websocket Client.", 0);
            }
        }
    }
}
