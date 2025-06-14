using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Globalization;
using ShimamuraBot.Classes;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices.JavaScript;
using System.Diagnostics;
using static System.Collections.Specialized.BitVector32;
using ShimamuraBot.Classes.Interface;

namespace ShimamuraBot
{
    /// <summary>
    ///  Major refactoring underway, this needs to be a semi-generic.
    /// </summary>
    //class WebsocketClient : IDisposable
    internal class WebsocketClient<T> where T : IWebSocketService
    {
        /* WHY DID I KEEP IT, BECAUSE IF UCKING LOVE RFCS THAT'S WHY
         * RFC 6455 REF
         * Closure:
         * The Close frame contains an opcode of 0x8.
         * The application MUST NOT send any more data frames after sending a Close frame.
         *  If there is a body, the first two bytes of
         *  the body MUST be a 2-byte unsigned integer (in network byte order)
         *  representing a status code with value /code/ defined in Section 7.4.
         *  Following the 2-byte integer, the body MAY contain UTF-8-encoded data
         *  with value /reason/
         *  
         * > 1000 indicates a normal closure, meaning that the purpose for which the connection was established has been fulfilled.
         * 1001 indicates that an endpoint is "going away", such as a server going down or a browser having navigated away from a page.
         * 1002 indicates that an endpoint is terminating the connection due to a protocol error.
         * 1003 indicates that an endpoint is terminating the connection because it has received a type of data it cannot accept (e.g., an endpoint that understands only text data MAY send this if it receives a binary message).
         * 1004 Reserved.  The specific meaning might be defined in the future.
         * 1005 is a reserved value and MUST NOT be set as a status code in a Close control frame by an endpoint.  It is designated for use in applications expecting a status code to indicate that no status code was actually present.
         * 1006 is a reserved value and MUST NOT be set as a status code in a Close control frame by an endpoint.  It is designated for use in applications expecting a status code to indicate that the connection was closed abnormally, e.g., without sending or receiving a Close control frame.
         * > 1007 indicates that an endpoint is terminating the connection because it has received data within a message that was not consistent with the type of the message (e.g., non-UTF-8 [RFC3629] data within a text message).
         * 1008 indicates that an endpoint is terminating the connection because it has received a message that violates its policy.  This is a generic status code that can be returned when there is no other more suitable status code (e.g., 1003 or 1009) or if there is a need to hide specific details about the policy.
         * 1009 indicates that an endpoint is terminating the connection because it has received a message that is too big for it to process.
         * 1010 indicates that an endpoint (client) is terminating the connection because it has expected the server to negotiate one or more extension, but the server didn't return them in the response message of the WebSocket handshake.  The list of extensions that
         */
        private string name = "WebSocket";
        private int _max_retry = 5;
        private int _retry = 0;

        /* Atomics */
        private int _connecting = 0;
        private int _reconnecting = 0;
        //private int _cancelled = 0;
        private bool _connected = false;

        private Uri _wss_endpoint { get; set; }
        private string channelId { get; set; } = string.Empty;
        private bool _faulted { get; set; } = false;
        private Stopwatch _cooldown { get; set; } = new();

        private Dictionary<int, string> chatHistory = new Dictionary<int, string>(); //For message deletion, muting, and blocking (severe)
        private List<string> chatHistory2 = new List<string>();

        /// <remarks>see <see cref="sendMessage(string, string, string, string)"/> method.</remarks>
        private SemaphoreSlim messageSemaphore = new SemaphoreSlim(1, 1);

        private ClientWebSocket? socket;
        private CancellationTokenSource? Cancellation = new CancellationTokenSource();

        #region New attempt
        private readonly T _service; // Joystick.WebSocket 
        private CancellationTokenSource _cts;
        /// <summary>I just really want to play BG3 ...................................................................................................</summary>

        public WebsocketClient(T Service, string endpoint, string auth, CancellationTokenSource cts) { // fukin block. I .... nothingness
            // okay, what now dumbass, we have methods, but what next
            _service = Service;
            _wss_endpoint = new Uri(endpoint); // THIS RIGHT HERE attach no fuck, maybe, no fuck, logging FUCK FUCKF fuck ffuck fuckity fuck
            _cts = cts;


            Service.RegisterSend(Send); // this allows me to invoke Send, but not socket context, starting and stopping......
            Service.RegisterConnectAsync(ConnectAsyncCC);
            Service.RegisterCloseAsync(CloseAsyncCC);
            // I need endpoint
            // I need credneitals.
        }

        public async Task<bool> Send(string msg) {
            // DONT FUCKING WORRY ABOUT IT
            bool LICKATACO = true;

            return LICKATACO;
        }

        public async Task<bool> CloseAsyncCC() {
            bool LICKATACO = true;

            return LICKATACO;
        }

        public async Task<bool> ConnectAsyncCC() {
            bool LICKATACO = true;
            return LICKATACO;
        }

        private async Task WebSocket_ReaderV3() { // IDFC the naming is coming back -> 2 -> 1.3 -> 3 See now it works
            if (Volatile.Read(ref _connecting) != 1) { return; }
            string name = $"{this.name}:ReaderV1.3";
            Print(name, $"Starting the WebSocket Reader. (Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Debug);

            try {
                if (socket == null)
                    throw new BotException(name, "Socket was a null reference.");

                await socket.ConnectAsync(_wss_endpoint, _cts.Token);
                _connected = true;
                _faulted = true;

                // This allows for a 20% buffer overhead for the WORST case scenario a bot can send 580 characters * 3 (if all 3 byte characters).
                byte[] buffer = new byte[8192];

                WebSocketReceiveResult socketReceive;

                //Websocket Reader Loop
                while (socket.State == WebSocketState.Open) {
                    socketReceive = await socket.ReceiveAsync(buffer, _cts.Token); //default is intentional - byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented


                    if (socketReceive.MessageType == WebSocketMessageType.Text) {
                        _ = _service.Receive(Encoding.UTF8.GetString(buffer, 0, socketReceive.Count)); // blah blah concurrentqueue or some shit
                        //continue;
                    } else if (socketReceive.MessageType == WebSocketMessageType.Close) {
                        switch ((int?)socketReceive.CloseStatus) {
                            case 1000: Print(name, $"Socket to {_service.Internal_Host} closed. (Normal Closure)", PrintSeverity.Normal); _faulted = false; return;
                            case 1002 or 1007 or 1008: _faulted = false; break;
                        }
                        Print(name, $"The socket to {_service.Internal_Host} was terminated. (State: {(int?)socketReceive.CloseStatus ?? 1006})", PrintSeverity.Warn);
                        break;
                    }

                    _cts.Token.ThrowIfCancellationRequested(); // According to microsofts this is much faster and less overhead than IsCancellationRequested. https://medium.com/@mitesh_shah/a-deep-dive-into-c-s-cancellationtoken-44bc7664555f
                }
            } catch (OperationCanceledException) {

            } catch (System.Net.WebSockets.WebSocketException) {
                new BotException(name, $"Connection to {WSS_HOST} was lost. (State: {(int?)socket!.CloseStatus ?? 1006}, Thread: {Environment.CurrentManagedThreadId})");
            } catch (Exception ex) {
                new BotException(name, $"Unhandled Exception (Thread: {Environment.CurrentManagedThreadId})", ex);
            } finally {
                _connected = false;
                Interlocked.Exchange(ref _connecting, 0);

                if (_faulted) {
                    Print(name, $"Socket fault detected. Reconnection will be attempted to restore the connection.", PrintSeverity.Debug);
                    _ = Reconnect();
                }
            }

            //_ = _service.Disconnect(true);
        }










        #endregion



        /// <summary>
        ///  Constructs the WebSocket client
        /// </summary>
        /// <param name="_channel_id">String - channel ID from the JWT class</param>
        /// <param name="Modules">Experimental - Setup Modules for use by the WebSocket Reader</param>
        public WebsocketClient(string _channel_id, Service service, string Modules = "vnyan,") {
            channelId = _channel_id;
            //_service = service;
            name = $"WebSocket:{service}";

            if(service == Service.Joystick)
                _wss_endpoint = new Uri($"{WSS_HOST}?token={Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"))}");
            else {
                _wss_endpoint = new Uri($"T");
            }

            /// This is here for when more than 1 Module is loaded, such as VTS, or shit idk make something.
            /*foreach (var Module in Modules.Split(',')) { // mock-up for module loading.
                if (string.IsNullOrEmpty(Module)) continue;

                switch (Module.ToLower()) {
                    case "vnyan": vCat = new VNyan(); break;
                    default: break;
                }
            }*/

            // why such construct before program starts. WHY SUCH? HODL it might return as a debug when it's moved.

            //Print($"{this.name}:_constructor", $"WebSocketClient constructed.", PrintSeverity.Debug);
        }

        ~WebsocketClient() {
            Dispose();
        }

        public void Dispose() {
            if (Cancellation != null)
                Cancellation.Dispose();
            if (socket != null)
                socket.Dispose();
            _cooldown = null!;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  Set the streamers channelid for sending messages/whispers - Sideload channelId
        /// </summary>
        /// <param name="_channel_id">String - Channel ID</param>
        public void SetChannelId(string _channel_id) => channelId = _channel_id;

        // rrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrrwhat?
        public void SetModules(string[] modules) {

        }


        /// <summary>
        ///  Starts the WebSocket Client and connects to WSS_HOST endpoint
        /// </summary>
        /// <returns>Boolean - True:Connected && Subscribed || False:Failure</returns>
        public async Task<bool> ConnectAsync(bool reconnect = false) {
            if (Interlocked.CompareExchange(ref _connecting, 0, 0) == 1 || _connected) { Print($"{this.name}:Connect", $"Socket is already in use. (Connected: {_connected}, Connecting: {_connecting}, Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Warn); return false; }
            Interlocked.Exchange(ref _connecting, 1);

            if (!reconnect && Connectivity.NoPing()) { // This is fine until you adjust to Ping change, it will attempt multiple times.
                Print($"{this.name}:Connect", $"Unable to detect internet connectivity.", PrintSeverity.Error);
                Interlocked.Exchange(ref _connecting, 0);
                return false;
            }

            if (Cancellation == null || Cancellation.IsCancellationRequested) {
                Cancellation?.Dispose();
                Cancellation = new();
            }

            Print($"{this.name}:Connect", $"Attempting to open a new Websocket Connection to {WSS_HOST}. (Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Debug);

            if (socket != null) { // Welcome back. The socket needs to be disposed to prevent mismatch state. Yes it happens a lot.
                socket.Abort();
                socket.Dispose();
            }

            socket = new ClientWebSocket();
            if (_service.Service == Service.Joystick) socket.Options.AddSubProtocol("actioncable-v1-json");

            _ = WebsocketReaderV1_3();

            if (await SocketOpen()) {
                Print($"{this.name}:Connect", $"Sending 'subscribe' to WebSocket endpoint.", PrintSeverity.Debug);
                Interlocked.Exchange(ref _reconnecting, 0);
                _retry = 0;
                return await sendMessage("subscribe");
            }

            Cancellation?.Cancel(); // this was commented out, if you find yourself here you know why. I think this should happen on failure to make sure, the socket isn't in some kind of hung state
            return false;
        }


        /// <summary>
        ///  Prevent mass flood of Connect attempts by slowing down the flow each error.
        /// </summary>
        private async Task Reconnect() {
            if(Interlocked.CompareExchange(ref _reconnecting, 0, 0) == 1) { Print($"{this.name}:Reconnect", $"Reconnect is already in progress.", PrintSeverity.Debug); return; }
            Interlocked.Exchange(ref _reconnecting, 1);

            if (Connectivity.NoPing()) {
                Print($"{this.name}:Reconnect", $"Could not detect internet connection, waiting for connectivity before attempting reconnection.", PrintSeverity.Debug);
                if (!await WaitForConnectivityAsync())
                    return;
                Print($"{this.name}:Reconnect", $"Connectivity re-established.", PrintSeverity.Debug);
            }

            Print($"{this.name}:Reconnect", $"Reconnecting to {WSS_HOST}.", PrintSeverity.Normal);

            while(_retry++ < _max_retry) {
                if (await ConnectAsync())
                    return;
                await Task.Delay(500);
            }
            new BotException($"{this.name}:Reconnect", $"Could not re-establish connection with {WSS_HOST}");
        }


        private async Task<bool> WaitForConnectivityAsync() {
            if (Cancellation == null) return false;
            try { while (true) {
                    Cancellation.Token.ThrowIfCancellationRequested();
                    if (Connectivity.Ping().Result)
                        return true;
                    await Task.Delay(500); // this should be a variable so poorer connections can set it higher.
                } } catch (OperationCanceledException) { } catch (Exception ex) { new BotException(name, $"Error while waiting for connectivity.", ex); }
            return false;
        }


        /// <summary>
        ///  Gracefully closes the Websocket Client
        /// </summary>
        /// <returns>Bool - True:Closed_Grace, False:Timeout</returns>
        /// <remarks>This will attempt to close the WebSocket but in rare cases can timeout, it will normally return True. Cases checking for socket re-use should not try re-use</remarks>
        public async Task<bool> CloseAsync() {
            string name = $"{this.name}:CloseAsync";

            try {
                if (socket?.State == WebSocketState.Open) {
                    Print(name, $"Sent goodbye message to the socket.", PrintSeverity.Debug);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default);
                } else { Print(name, $"Socket was already closed.", PrintSeverity.Debug); }
            } catch (Exception ex) {
                new BotException(name, "Error when closing the WebSocket.", ex);
            }

            Print(name, $"Socket Status: {await SocketOpen(-1)}", PrintSeverity.Debug);

            Cancellation?.Cancel();

            if (!await SocketOpen(-1)) { // Semantics might seem off but yes socket is supposed to return a true for closure with op -1
                new BotException(name, "The socket did not close within the expected time (Timeout)");
                return false;
            }

            Print(name, $"Bot stopped succesfully.", PrintSeverity.Normal);

            return true;
        }


        /// <summary>
        ///  Returns the status of the socket
        /// </summary>
        /// <param name="code"><see cref="int"/> -1 to check closure, otherwise leave it default</param>
        /// <returns>Bool - True:Availabe:Closed, False:Timeout:Timeout</returns>
        public async Task<bool> SocketOpen(int code = 0) { // This is very slow. Handshake?
            if (socket == null) { Print($"{this.name}:SocketState", $"Socket is null.", PrintSeverity.Debug); return false; }
            string name = $"{this.name}:SocketState";

            Print(name, $"Attempting to assert socket status.", PrintSeverity.Debug);
            Stopwatch timeout = Stopwatch.StartNew();

            while (timeout.ElapsedMilliseconds < 3_333) {
                switch(socket?.State) {
                    case WebSocketState.Open: return true;
                    case WebSocketState.CloseSent or WebSocketState.Closed or WebSocketState.Aborted or WebSocketState.CloseReceived: if (code < 0) return true; break;
                }
                await Task.Delay(6);
            }
            Print(name, $"Socket Timed-out while checking state.", PrintSeverity.Debug);
            return false;
        }


        /// <summary>
        ///  Returns the socket status
        /// </summary>
        /// <returns><see cref="bool"/> True:Connected, False:Disconnected</returns>
        public bool Open => socket != null && socket.State == WebSocketState.Open;// (_connected && Interlocked.CompareExchange(ref _cancelled, 0, 0) != 1);
        //private bool IsCancelled => Interlocked.CompareExchange(ref _cancelled, 0, 0) == 1;
        // 1, 0, 0 x<-y=false=1 != 1 false // TRUE && (TRUE && FALSE) == FALSE
        // 0, 0, 0 X<-y=false=0 == 1 false

        public string[] getMessage(int id) // I think this for FAIL2BAN, err I mean banning/deleting.
        {
            string[] kvipairs = new string[] { "", "" };
            try
            {
                if (string.IsNullOrEmpty(chatHistory2[id]))
                {
                    //kvipairs[0] = 
                }
            } catch (Exception ex)
            {
                var a = chatHistory[id];
            }


            return kvipairs;
        }


        


        

        public async Task<bool> SendMessage(string action, string msg) => await sendMessage(action, msg, "", ""); // Do you really need to know? ehh iffy check references
        public async Task<bool> SendWhisper(string action, string msg, string user) => await sendMessage(action, msg, user, "");
        public async Task<bool> Mute_User(string action, string msgid) => await Task.FromResult(true);
        public async Task<bool> Unmute_User(string action, string user) => await Task.FromResult(true);
        public async Task<bool> Block_User(string action, string msgid) {
            while(true) { // problem is this could get flushed off the buffer before seen if messages are incoming. how handle
                Print("Blocking", $"This action is severe, to confirm please make sure username/msgid is correct. MessageID: {msgid} (y/n)", PrintSeverity.Error); // maybe capture username too.
                var a = Console.ReadLine()?.ToLower();
                if(a == "y" || a == "n") {
                    if(a == "n") {
                        Print("Blocking", "No action taken.", PrintSeverity.None);
                        return false;
                    }

                    return await sendMessage(action,"","",msgid);
                }
            }
        }


        // okay we know this works, now how do I inject my bytes into the byte array
        // HACK THE PLANET, I mean hijack the byte array. - Figure it out using strings, but hey I'm leaving this cause it's fucking hilarious. to me at least
        public async Task<bool> RAGEAGAINSTPIZZAHUT() {
            bool _success = false;

            //https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
            //Attempting to invoke any other operations in parallel may corrupt the instance.
            //Attempting to invoke a send operation while another is in progress or a receive operation while another is in progress will result in an exception.
            await messageSemaphore.WaitAsync();
            try {
                if (socket != null && socket.State == WebSocketState.Open) {
                    //crazy right? I'm fucking insane I don't know.
                    byte[] msg = [0x7B, 0X22, 0X63, 0X6F, 0X6D, 0X6D, 0X61, 0X6E, 0X64, 0X22, 0X3A, 0X22, 0X6D, 0X65, 0X73, 0X73, 0X61, 0X67, 0X65, 0X22, 0X2C, 0X22, 0X69, 0X64, 0X65, 0X6E, 0X74, 0X69, 0X66, 0X69, 0X65, 0X72, 0X22, 0X3A, 0X22, 0X7B, 0X5C, 0X22, 0X63, 0X68, 0X61, 0X6E, 0X6E, 0X65, 0X6C, 0X5C, 0X22, 0X3A, 0X5C, 0X22, 0X47, 0X61, 0X74, 0X65, 0X77, 0X61, 0X79, 0X43, 0X68, 0X61, 0X6E, 0X6E, 0X65, 0X6C, 0X5C, 0X22, 0X7D, 0X22, 0X2C, 0X22, 0X64, 0X61, 0X74, 0X61, 0X22, 0X3A, 0X22, 0X7B, 0X5C, 0X22, 0X61, 0X63, 0X74, 0X69, 0X6F, 0X6E, 0X5C, 0X22, 0X3A, 0X20, 0X5C, 0X22, 0X73, 0X65, 0X6E, 0X64, 0X5F, 0X6D, 0X65, 0X73, 0X73, 0X61, 0X67, 0X65, 0X5C, 0X22, 0X2C, 0X5C, 0X22, 0X74, 0X65, 0X78, 0X74, 0X5C, 0X22, 0X3A, 0X20, 0X5C, 0X22, 0X48, 0X65, 0X6C, 0X6C, 0X6F, 0X20, 0X57, 0X6F, 0X72, 0X6C, 0X64, 0X20, 0xF0, 0x9F, 0x92, 0x9C, 0X5C, 0X22, 0X2C, 0X5C, 0X22, 0X63, 0X68, 0X61, 0X6E, 0X6E, 0X65, 0X6C, 0X49, 0X64, 0X5C, 0X22, 0X3A, 0X20, 0X5C, 0X22, 0X34, 0X37, 0X30, 0X61, 0X34, 0X36, 0X38, 0X37, 0X39, 0X32, 0X34, 0X66, 0X39, 0X35, 0X36, 0X31, 0X62, 0X35, 0X35, 0X66, 0X39, 0X39, 0X30, 0X63, 0X36, 0X65, 0X36, 0X32, 0X34, 0X38, 0X30, 0X30, 0X63, 0X37, 0X31, 0X30, 0X38, 0X31, 0X30, 0X39, 0X65, 0X38, 0X34, 0X66, 0X63, 0X38, 0X38, 0X65, 0X30, 0X35, 0X39, 0X38, 0X64, 0X36, 0X34, 0X31, 0X65, 0X33, 0X36, 0X62, 0X37, 0X65, 0X39, 0X66, 0X5C, 0X22, 0X7D, 0X22, 0X7D];

                    await socket.SendAsync(msg, WebSocketMessageType.Text, true, default); //byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    _success = true;
                } else
                    throw new BotException(this.name, "Socket status was not connected or unobtainable while trying to send a message.");
            } catch (WebSocketException wse) {
                new BotException(this.name, $"Could not send message", wse);
            } catch (Exception ex) {
                new BotException(this.name, $"Unhandled Exception", ex);
            } finally { //https://stackoverflow.com/a/10260233
                messageSemaphore.Release();
            }

            return _success;
        }


        /// <summary>
        ///  Websocket Writer - Send message to the socket and wait for success or failure
        /// </summary>
        /// <remarks>Do not call directly to this Task, use the proper calls.<br />Example:<br />- SendMessage(),<br />- SendWhisper(),<br />- Mute_User(),<br />- Block_User()</remarks>
        /// <param name="action">The action. (Alternatively for subscription 'subscribe')</param>
        /// <param name="msg">Message to send</param>
        /// <param name="user">Username - used for unmute and whispers</param>
        /// <param name="msgid">Message Identifier - used for muting and blocking</param>
        /// <returns>Bool - True if sent, False with error</returns>
        private async Task<bool> sendMessage(string action, string msg="", string user="", string msgid="") { // You are my last hope to save me from myself.
            bool _success = false;

            //https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
            //Attempting to invoke any other operations in parallel may corrupt the instance.
            //Attempting to invoke a send operation while another is in progress or a receive operation while another is in progress will result in an exception.
            await messageSemaphore.WaitAsync();
            try {
                if (socket != null && socket.State == WebSocketState.Open) {
                    var formatted_msg = Joystick.WebSocket.MessageConstructor(action, msg, user, msgid);

                    await socket.SendAsync(Encoding.UTF8.GetBytes(formatted_msg), WebSocketMessageType.Text, true, default); //byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    _success = true;
                } else
                    throw new BotException(this.name, "Socket status was not connected or unobtainable while trying to send a message.");
            } catch (WebSocketException wse) {
                new BotException(this.name, $"Could not send message: {action} :: msg: {msg} :: user: {user} :: messageId: {msgid}", wse);
            } catch (Exception ex) {
                new BotException(this.name, $"Unhandled Exception", ex);
            } finally { //https://stackoverflow.com/a/10260233
                messageSemaphore.Release();
            }

             return _success;
        }


        private async Task WebsocketReaderV1_3() {
            string name = $"{this.name}:ReaderV1.3";
            Print(name, $"Starting the WebSocket Reader. (Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Debug);

            try {
                await socket.ConnectAsync(_wss_endpoint, Cancellation.Token); // fuck your null reference you don't even get a 
                _connected = true;
                _faulted = true;

                // This allows for a 20% buffer overhead for the WORST case scenario a bot can send 580 characters * 3 (if all 3 byte characters).
                byte[] buffer = new byte[8192];

                WebSocketReceiveResult socketReceive;

                //Websocket Reader Loop
                while (socket.State == WebSocketState.Open) {
                    socketReceive = await socket.ReceiveAsync(buffer, Cancellation.Token); //default is intentional - byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented


                    if (socketReceive.MessageType == WebSocketMessageType.Text) {
                        _ = onMessage(Encoding.UTF8.GetString(buffer, 0, socketReceive.Count));
                        //continue;
                    } else if (socketReceive.MessageType == WebSocketMessageType.Close) {
                        switch ((int?)socketReceive.CloseStatus) {
                            case 1000: Print(name, $"Socket to {WSS_HOST} closed. (Normal Closure)", PrintSeverity.Normal); _faulted = false; return;
                            case 1002 or 1007 or 1008: _faulted = false; break;
                        }
                        Print(name, $"The socket to {WSS_HOST} was terminated. (State: {(int?)socketReceive.CloseStatus ?? 1006})", PrintSeverity.Warn);
                        break;
                    }

                    Cancellation.Token.ThrowIfCancellationRequested(); // According to microsofts this is much faster and less overhead than IsCancellationRequested. https://medium.com/@mitesh_shah/a-deep-dive-into-c-s-cancellationtoken-44bc7664555f
                }
            } catch (OperationCanceledException) {
                
            } catch (System.Net.WebSockets.WebSocketException) {
                new BotException(name, $"Connection to {WSS_HOST} was lost. (State: {(int?)socket!.CloseStatus ?? 1006}, Thread: {Environment.CurrentManagedThreadId})");
            } catch (Exception ex) {
                new BotException(name, $"Unhandled Exception (Thread: {Environment.CurrentManagedThreadId})", ex);
            } finally {
                _connected = false;
                Interlocked.Exchange(ref _connecting, 0);

                if (_faulted) {
                    Print(name, $"Socket fault detected. Reconnection will be attempted to restore the connection.", PrintSeverity.Debug);
                    _ = Reconnect();
                }
            }
        }


        /// <summary>
        ///  Websocket Reader - Connects if not connected and waits for socket messages 
        /// </summary>
        /// <returns></returns>
        /*private async Task WebsocketReader() {
            string name = $"{this.name}:Reader";
            Print(name, $"Starting the WebSocket Reader. (Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Debug);

            try {
                if (socket == null) throw new BotException(name, $"Thrown: Socket was null.");
                await socket.ConnectAsync(_wss_endpoint, default);
                _connected = true;
                _faulted = true;
                Interlocked.Exchange(ref _connecting, 0);

                // was able to send 1200 bytes, I don't know the upper limit of char count but a minimum of 1200 bytes can be achieved.
                byte[] buffer = new byte[2048]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
                Task<WebSocketReceiveResult> socketMsg;
                WebSocketReceiveResult socketResult;
                Task? TaskTriggered;

                //Websocket Reader Loop
                while (socket.State == WebSocketState.Open && !Cancellation.IsCancellationRequested) {
                    socketMsg = socket.ReceiveAsync(buffer, default); //default is intentional - byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    TaskTriggered = await Task.WhenAny(Task.Delay(Timeout.Infinite, Cancellation.Token), socketMsg); // Wait with a GOTO #ID, waiting to jump to either Timeout or SocketMsgReceived.

                    if (TaskTriggered != socketMsg) break;

                    socketResult = await socketMsg;

                    if (socketResult.MessageType == WebSocketMessageType.Text) {
                        _ = onMessage(Encoding.UTF8.GetString(buffer, 0, socketResult.Count));
                        continue;
                    } else if (socketResult.MessageType == WebSocketMessageType.Close) {
                        switch ((int?)socketResult.CloseStatus) { case 1000 or 1002 or 1007 or 1008: _faulted = false; break; }
                        Print(name, $"The socket to {WSS_HOST} was terminated. (State: {(int?)socketResult.CloseStatus ?? 1006})", PrintSeverity.Warn);
                        break;
                    }
                }

                /// This is a Normal closure block.
                if (Cancellation.IsCancellationRequested) {
                    if (socket.State == WebSocketState.Open) { Print(name, $"Sent goodbye message to the socket.", PrintSeverity.Debug); await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default); }
                    _faulted = false;
                    Print(name, $"Socket to {WSS_HOST} closed. (Normal Closure)", PrintSeverity.Normal);
                }
            } catch (System.Net.WebSockets.WebSocketException) {
                new BotException(name, $"Connection to {WSS_HOST} was lost. (State: {(int?)socket!.CloseStatus ?? 1006}, Thread: {Environment.CurrentManagedThreadId})");
            } catch (Exception ex) {
                new BotException(name, $"Unhandled Exception (Thread: {Environment.CurrentManagedThreadId})", ex);
            } finally {
                /// This can probably be removed now, I was just making sure that there was a weird state which required resetting the socket.
                //_ = Logger.LogAsync(name, new string[] { $"Socket Closed. Additional Information :: State: {socket!.State}", $" | CloseStatus: {(int?)socket.CloseStatus ?? 1006} | _faulted: {_faulted} | _cancelled: {Cancellation.IsCancellationRequested} | connected: {_connected} | connecting: {_connecting} | reconnecting: {_reconnecting}" });
                if(!Cancellation.IsCancellationRequested && !_faulted) {
                    Print(name, $"Abnormal closure detected. (State: {(int?)socket!.CloseStatus ?? 1006})", PrintSeverity.Debug);
                }
                _connected = false;
                Interlocked.Exchange(ref _connecting, 0);

                if (_faulted) {
                    //await Task.Delay(120);
                    Print(name, $"Socket fault detected. Reconnection will be attempted to restore the connection.", PrintSeverity.Debug);
                    _ = Reconnect();
                }
            }
        }*/

        #region JSONClass

        #region Presence
        public class PresenceMessage
        {
            public required string id { get; set; }
            public required string @event { get; set; }
            public required string type { get; set; }
            public string? text { get; set; }
            public required string channelId { get; set; }
            public required DateTime createdAt { get; set; }
        }

        public class RootPresenceEvent
        {
            public required string identifier { get; set; }
            public required PresenceMessage message { get; set; }
        }
        #endregion

        #region StreamEvents
        public class RootStreamEvents
        {
            public required string identifier { get; set; }
            public required Message message { get; set; }
        }

        public class Message
        {
            public required string id { get; set; }
            public required string @event { get; set; }
            public required string type { get; set; }
            public string? text { get; set; }
            //private MetadataObject _metadata { get; set; }
            //public string? metadata { get; set; }
            public required DateTime createdAt { get; set; }
            public required string channelId { get; set; }


            private MetadataObject? _metadata;
            private string? _string_meta_data;

            public MetadataObject Metadata => _metadata!;
            [JsonPropertyName("metadata")]
            public string raw_metadata {
                get => _string_meta_data ?? string.Empty;
                set {
                    _string_meta_data = value;
                    _metadata = JsonSerializer.Deserialize<MetadataObject>(_string_meta_data);
                }
            }

            /// <summary>
            /// [JsonIgnore] WHAT THE FUCK 
            /// </summary>
            //public MetadataObject metadataObject => JsonSerializer.Deserialize<MetadataObject>(metadata);
        }

        public class MetadataObject
        {
            public string? who { get; set; }
            public string? what { get; set; }
            [JsonPropertyName("how_much")]
            public int? howMuch { get; set; }
            [JsonPropertyName("tip_menu_item")]
            public string? tipMenuItem { get; set; }
            public string? prize { get; set; }
            [JsonPropertyName("number_of_viewers")]
            public int? viewerCount { get; set; }
        }
        #endregion

        #region MessageEvent
        public class RootMessageEvent
        {
            [JsonPropertyName("identifier")]
            public required string identifier { get; set; }

            [JsonPropertyName("message")]
            public required ChatMessage message { get; set; }
        }

        public class ChatMessage
        {
            [JsonPropertyName("event")]
            public required string @event { get; set; }

            [JsonPropertyName("createdAt")]
            public required DateTime createdAt { get; set; }

            [JsonPropertyName("messageId")]
            public required string messageId { get; set; }

            [JsonPropertyName("type")]
            public required string type { get; set; }

            [JsonPropertyName("visibility")]
            public required string visibility { get; set; }

            [JsonPropertyName("text")]
            public required string text { get; set; }

            [JsonPropertyName("botCommand")]
            public string? botCommand { get; set; }

            [JsonPropertyName("botCommandArg")]
            public string? botCommandArg { get; set; }

            [JsonPropertyName("emotesUsed")]
            public List<object>? emotesUsed { get; set; }

            [JsonPropertyName("author")]
            public required ChatUser author { get; set; }

            [JsonPropertyName("streamer")]
            public required ChatUser streamer { get; set; }

            [JsonPropertyName("channelId")]
            public required string channelId { get; set; }

            [JsonPropertyName("mention")]
            public required bool mention { get; set; }

            [JsonPropertyName("mentionedUsername")]
            public string? mentionedUsername { get; set; }
            [JsonPropertyName("highlight")]
            public required bool Highlighted { get; set; }
        }

        public class ChatUser
        {
            [JsonPropertyName("slug")]
            public required string slug { get; set; }

            [JsonPropertyName("username")]
            public required string username { get; set; }

            [JsonPropertyName("usernameColor")]
            public object? usernameColor { get; set; }

            [JsonPropertyName("displayNameWithFlair")]
            public string? displayNameWithFlair { get; set; }

            [JsonPropertyName("signedPhotoUrl")]
            public string? signedPhotoUrl { get; set; }

            [JsonPropertyName("signedPhotoThumbUrl")]
            public string? signedPhotoThumbUrl { get; set; }

            [JsonPropertyName("isStreamer")]
            public bool? isStreamer { get; set; }

            [JsonPropertyName("isModerator")]
            public bool? isModerator { get; set; }

            [JsonPropertyName("isSubscriber")]
            public bool? isSubscriber { get; set; }
        }
        #endregion

        #endregion
    }
}
