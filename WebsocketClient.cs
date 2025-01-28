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

namespace ShimamuraBot
{
    class WebsocketClient
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
        private string name = "Websocket";
        private int _max_retry = 5;
        private int _retry = 0;

        /* Atomics */
        private int _connecting = 0;
        private int _reconnecting = 0;
        private int _cancelled = 0;
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
        //private CancellationTokenSource Cancellation = new CancellationTokenSource();

        //modules need to be instantiated here for access, maybe there is reason for constructor to pass which modules to load.
        private VNyan? vCat;


        /// <summary>
        /// Constructs the WebSocket client
        /// </summary>
        /// <param name="_channel_id">String - channel ID from the JWT class</param>
        /// <param name="Modules">Experimental - Setup Modules for use by the WebSocket Reader</param>
        public WebsocketClient(string _channel_id, string Modules = "vnyan,") {
            channelId = _channel_id;

            _wss_endpoint = new Uri($"{WSS_HOST}?token={Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"))}");

            foreach (var Module in Modules.Split(',')) { // mock-up for module loading.
                if (string.IsNullOrEmpty(Module)) continue;

                switch (Module.ToLower()) {
                    case "vnyan": vCat = new VNyan(); break;
                    default: break;
                }
            }

            //socket.Options.AddSubProtocol("actioncable-v1-json");

            Print($"{this.name}:_constructor", $"WebSocketClient constructed.", PrintSeverity.Debug);
        }


        /// <summary>
        ///  Set the streamers channelid for sending messages/whispers
        /// </summary>
        /// <param name="_channel_id">String - Channel ID</param>
        public void SetChannelId(string _channel_id) => channelId = _channel_id;


        /// <summary>
        ///  Starts the WebSocket Client and connects to WSS_HOST endpoint
        /// </summary>
        /// <returns>Boolean - True:Connected && Subscribed || False:Failure</returns>
        public async Task<bool> Connect(bool reconnect = false) {
            if (Interlocked.CompareExchange(ref _connecting, 0, 0) == 1 || _connected) { Print($"{this.name}:Connect", $"Socket is already in use. (Connected: {_connected}, Connecting: {_connecting}, Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Warn); return false; }
            Interlocked.Exchange(ref _connecting, 1);

            if (!reconnect && Connectivity.NoPing()) {
                Print($"{this.name}:Connect", $"Unable to detect internet connectivity.", PrintSeverity.Error);
                Interlocked.Exchange(ref _connecting, 0);
                return false;
            }

            //if (Interlocked.CompareExchange(ref _cancelled, 1, 0) == 1) // set isCancelled back to false.
                Interlocked.Exchange(ref _cancelled, 0);

            //if (Cancellation.IsCancellationRequested)
                //if(!Cancellation.TryReset()) { Cancellation.Dispose(); Cancellation = new(); }

            Print($"{this.name}:Connect", $"Attempting to open a new Websocket Connection to {WSS_HOST}. (Thread: {Environment.CurrentManagedThreadId})", PrintSeverity.Debug);

            if (socket != null) { // Welcome back. The socket needs to be disposed to prevent mismatch state. Yes it happens a lot.
                //Print(this.name, $"Disposing an old WebSocket before continuing.", PrintSeverity.Debug);
                socket.Abort();
                socket.Dispose();
            }

            socket = new ClientWebSocket();
            socket.Options.AddSubProtocol("actioncable-v1-json");

            _ = WebsocketReaderV2();

            if (await SocketOpen()) {
                Print($"{this.name}:Connect", $"Sending 'subscribe' to WebSocket endpoint.", PrintSeverity.Debug);
                Interlocked.Exchange(ref _reconnecting, 0);
                _retry = 0;
                return await sendMessage("subscribe"); // this is a lot more pretty I like it
            }

            //Cancellation.Cancel();
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

            Print($"{this.name}:Reconnect", $"Attempting to re-establish connection with {WSS_HOST}.", PrintSeverity.Normal);

            while(_retry++ < _max_retry) {
                if (await Connect())
                    return;
                await Task.Delay(720);
            }
            new BotException($"{this.name}:Reconnect", $"Could not re-establish connection with {WSS_HOST}");
        }


        private async Task<bool> WaitForConnectivityAsync() {
            while(!IsCancelled) {
                if (Connectivity.Ping())
                    return true;
                await Task.Delay(500);
            }
            return false;
        }


        /// <summary>
        ///  Gracefully closes the Websocket Client
        /// </summary>
        /// <returns>Bool - True:Closed_Grace, False:Timeout</returns>
        /// <remarks>This will attempt to close the WebSocket but in rare cases can timeout, it will normally return True. Cases checking for socket re-use should not try re-use</remarks>
        public async Task<bool> Close() {
            Interlocked.Exchange(ref _cancelled, 1);

            if (!await SocketOpen(-1)) { // Semantics might seem off but yes socket is supposed to return a true for closure with op -1
                new BotException(this.name, "The socket did not close within the expected time (Timeout)");
                return false;
            }

            Print(this.name, $"Bot stopped succesfully.", PrintSeverity.Normal);

            return true;
        }


        private bool closure_status => socket != null && (
               socket.State == WebSocketState.Closed
            || socket.State == WebSocketState.Aborted
            || socket.State == WebSocketState.CloseReceived
        );


        /// <summary>
        ///  Returns the status of the socket
        /// </summary>
        /// <param name="code"><see cref="int"/> -1 to check closure, otherwise leave it default</param>
        /// <returns>Bool - True:Availabe:Closed, False:Timeout:Timeout</returns>
        private async Task<bool> SocketOpen(int code = 0) {
            string name = $"{this.name}:SocketState";

            Print(name, $"Attempting to assert socket status.", PrintSeverity.Debug);
            Stopwatch timeout = Stopwatch.StartNew();

            while (timeout.ElapsedMilliseconds < 3_333) {
                if (socket == null) {
                    Print(name, $"Socket is null.", PrintSeverity.Debug);
                    return false;
                }

                // Check for closure
                if (code < 0 && closure_status) {
                    Print(name, $"Socket is closed.", PrintSeverity.Debug);
                    return true;
                }

                // Check for open state
                if (code == 0 && socket.State == WebSocketState.Open) {
                    Print(name, $"Socket is Open.", PrintSeverity.Debug);
                    return true;
                }

                await Task.Delay(9);
            }
            Print(name, $"Socket Timed-out while checking state.", PrintSeverity.Debug);
            return false;
        }


        /// <summary>
        ///  Returns the socket status
        /// </summary>
        /// <returns><see cref="bool"/> True:Connected, False:Disconnected</returns>
        public bool Open => socket != null && (_connected && Interlocked.CompareExchange(ref _cancelled, 0, 0) != 1);
        private bool IsCancelled => Interlocked.CompareExchange(ref _cancelled, 0, 0) == 1;
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


        private Task onMessage_StreamEvent(string payload) { // I have no idea what I was smoking when I wrote this.
            RootStreamEvents? streamEvent;

            try { streamEvent = JsonSerializer.Deserialize<RootStreamEvents>(payload); } catch (Exception ex) { new BotException($"{this.name}:StreamEvent", $"Could not deserialize the WebSocket message.", ex); return Task.CompletedTask; }

            switch (streamEvent!.message.type) {
                case "Started":
                    // stream started
                    Print("NT", "Your stream is now live.", PrintSeverity.Normal);
                    return Task.CompletedTask;
                case "StreamEnding": // Stream ending (pending state? maybe for reconnection attempt?)
                    /// noop - for now.
                    return Task.CompletedTask;
                case "Ended": // Stream has ended
                    Print("NT", $"your stream has ended.", PrintSeverity.Normal);
                    return Task.CompletedTask;
                case "ViewerCountUpdated": // Polled maybe? otherwise on actual change. it looks like it can actually generate 2 different ID's and fire them both
                    Console.Title = $"♥ Shimamura :: {streamEvent.message.Metadata.viewerCount.ToString()} ♥";
                    return Task.CompletedTask;
                    /// noop
                case "SettingsUpdated":
                    /// noop - for now, I might link this to the API call.
                    return Task.CompletedTask;
                case "Tipped":
                    /// ===> This goes to Module eventually, for now create a class maybe or something to handle WebSocket connect to vNyan
                    /// This is also going to be the most tricky one to handle because you need to handle all client modules
                    /// assuming it is a 'Module' type tip.
                    var redeem = streamEvent.message.text ?? "Unknown redeem 'Text'";
                    var redeemed = streamEvent.message.Metadata.tipMenuItem;
                    var redeemer = streamEvent.message.Metadata.who;
                    var cost = streamEvent.message.Metadata.howMuch;

                    /// I think they split(' ', 2) tip items before sending over socket, reasoning:
                    /// "Remove Bra for the Entire Stream" is a tip item, however I received "Remove Bra"
                    /// This was long ago though I don't think I log tips anymore / haven't got a tip in a long time.
                    /// For now to make it easy, I'm only going to go by the tip_cost
                    /// Investimagate.


                    switch(cost) {
                        case 3:
                            _ = Redeemer("", "cumdump", true, 10, true);
                            break;
                        case 10:
                            _ = Redeemer("", "tits", true, 600, true); // no models has clothes right now until I fix Yuri so do not enable this redeem.
                            break;
                        case 15:
                            _ = Redeemer("", "eyes", true, 0);
                            break;
                        case 25:
                            _ = Redeemer("", "tits", true, 1800, true);
                            break;
                        case 100:
                            _ = Redeemer("", "tits", true);
                            break;
                        default:
                            if (cost > 30)
                                _ = SendMessage("send_message", $"Thank you for the tip ! If you have any requests let me know ^^ - A.S.");
                            else
                                // If this sends a purple heart on the first try, I'll flip my table. then unflip it.
                                //yeh doesn't work.
                                _ = SendMessage("send_message", $"Thank you for the tip {streamEvent.message.Metadata.who} ! \u1F49C"); // 1F49C is supposed to be purple heart, however GL having that render on a console buffer.
                            break;
                    }
                    return Task.CompletedTask;
                case "WheelSpinClaimed":
                    // Wheelspin tip - I do not believe you have implemnted any way of handling this yet, soo. DRAW THE FUCKING OWL
                    Print("", $"{streamEvent.message.Metadata.who} just spun the wheel and won {streamEvent.message.Metadata.prize} for {streamEvent.message.Metadata.howMuch} !", PrintSeverity.Normal);
                    // owl
                    break;
                case "Followed": // You haz new fren
                    _ = SendMessage("send_message", $"Welcome to the Cherry Blossoms {streamEvent.message.Metadata.who}. Thank you the Follow !");
                    Print("", $"A new follower has appeared! Say hi to {streamEvent.message.Metadata.who}!", PrintSeverity.Normal);
                    return Task.CompletedTask;
                case "FollowerCountUpdated":
                    // Noop - 
                    return Task.CompletedTask;
                case "DeviceConnected": // You haz device connected and reported back by API
                    Print("", $"Your toy was registered as `{streamEvent.message.text}` from Joystick", PrintSeverity.Normal);
                    // IDK probably not worth mentioning but I don't have a toy to test how connection works. If someone was actually running Shimararu it might be useful to know on the fly when it was registered.
                    return Task.CompletedTask;
                default:
                    Print($"{this.name}:StreamEvent", $"Received a new Event that is not handled! EXCITING!", PrintSeverity.Debug);
                    _ = Logger.LogAsync($"{this.name}:WebSocket:StreamEvent:Discover L I M P", new string[] { $"Unhandled StreamEvent Raw :: ", payload });
                    break;
            }

            // Found you, you little bugger you.
            // Discover L I M P

            _ = Logger.LogAsync("StreamEvent", new string[] { streamEvent.message!.text!, $"who: {streamEvent.message.Metadata.who} ::", $"what: {streamEvent.message.Metadata.what} :: tipmenitem: {streamEvent.message.Metadata.tipMenuItem} :: prize: {streamEvent.message.Metadata.prize} :: howMuch: {streamEvent.message.Metadata.howMuch} {Environment.NewLine}-> Payload: {payload}" });
            return Task.CompletedTask;
        }


        /// <summary>
        ///  Handles bang bot commands.
        /// </summary>
        /// <param name="message"><see cref="RootMessageEvent"/> deserialized message.</param>
        /// <returns></returns>
        private Task OnBangCommand(RootMessageEvent msg) {

            if (vCat != null) {
                var cmd = msg.message.text.Split('.')[1].ToLower();

                switch (cmd) {
                    case "duck" or "yeet":
                        vCat.Redeem(cmd);
                        break;
                    case "testing":
                        vCat.Redeem("tta");
                        break;
                }
            } else {
                new BotException($"{this.name}:CommandHandler", $"No instance of vNyan was found.");
            }

            return Task.CompletedTask;
        }


        private Task onMessage_Message(string payload) {
            RootMessageEvent? msg;

            try { msg = JsonSerializer.Deserialize<RootMessageEvent>(payload); } catch (Exception ex) { new BotException(name, $"Unable to deserialize OnMessage: {payload}", ex); return Task.CompletedTask; }

            ///chatHistory2.Add(msg.message.messageId);
            //if (chatHistory2[user_input])

            if (msg.message.text.ToLower().Contains("adachi91")) { if (_cooldown.IsRunning && _cooldown.ElapsedMilliseconds > 13_130) { _cooldown.Restart(); } else { _cooldown.Start(); if (WebUI!.Open && _cooldown.ElapsedMilliseconds < 13_000) _ = AudioOot.PlayAudioAsync(AudioOot.adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.HeyDumb, WebUI); } }
            if (msg.message.text.Contains("002") || msg!.message.text.Contains("zerotwo")) if (WebUI!.Open) WebUI.SendSSEImageAsync("https://steamuserimages-a.akamaihd.net/ugc/778494769436587920/675371BED432AF394DB2F145632671082F4779DF/?imw=5000\u0026imh=5000\u0026ima=fit\u0026impolicy=Letterbox\u0026imcolor=%23000000\u0026letterbox=false", 4); else new BotException(name, $"WebUI is not open.");
            if (msg.message.visibility != "public") { _ = Logger.LogAsync($"{this.name}:OnMessage", new string[] { $"Discover L I M P - NonPub msg: {payload}" }); return Task.CompletedTask; }// I think DM to bot only - not user. so this should be handled for bot-whisper interactions.
            if (msg.message.text.StartsWith('.')) { _ = OnBangCommand(msg); return Task.CompletedTask; }

            Print("Chat", $"{msg.message.author.username}: {msg.message.text}", PrintSeverity.Normal);

            
            _ = AudioOot.PlayAudioAsync(AudioOot.adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.Beep);
            _ = Logger.LogAsync("ChatMessage", new string[] { $"{msg.message.author.username}: {msg.message.text}" });

            return Task.CompletedTask;
        }


        private Task onMessage_PresenceEvent(string payload) {
            RootPresenceEvent? msg;

            try { msg = JsonSerializer.Deserialize<RootPresenceEvent>(payload); } catch (Exception ex) { new BotException($"{this.name}:OnPresence", $"Could not deserialize remote message: {payload}", ex); return Task.CompletedTask; }

            var eveType = msg!.message.type == "enter_stream" ? "Entered the chat" : "Left the chat";
            _ = Logger.LogAsync("UserPresence", new string[] { $"{msg.message.text} {eveType}" });
            return Task.CompletedTask;
        }


        private Task onMessage(string data) {
            if (String.Compare(data, 0, "{\"type\":\"ping\"", 0, 14, StringComparison.OrdinalIgnoreCase) == 0) return Task.CompletedTask; // why? BECAUSE I STILL FIND IT HILARIOUS

            if (data.Contains("confirm_subscription")) { // TODO better comparison other than "Contains"
                if(DEBUGGING_ENABLED /* DO NOT REMOVE THIS ONE. */ ) _ = Logger.LogAsync(this.name, new string[] { data });
                Print(this.name, $"Estasblished connection to chatroom.", PrintSeverity.Normal);
                return Task.CompletedTask;
            } else if (data.Contains("reject_subscription")) {
                Print(this.name, $"Could not connect to chat. Make sure everything is correctly configured.", PrintSeverity.Warn);
                return Task.CompletedTask;
            }

            if (!data.Contains("\"message\":")) return Task.CompletedTask;

            JsonNode jsonNode = JsonNode.Parse(data)!;

            string eventType = (string)jsonNode["message"]!["event"]!;

            switch (eventType) {
                case "StreamEvent": _ = onMessage_StreamEvent(data); break;
                case "ChatMessage": _ = onMessage_Message(data); break;
                case "UserPresence": _ = onMessage_PresenceEvent(data); break;
                default:
                    Print($"{this.name}:EventType", $"Unexpected request from remote host has been logged.", PrintSeverity.Debug);
                    _ = Logger.LogAsync($"{this.name}-EventType", new string[] { $"Unexpected Type from {WSS_HOST}", $"Event={eventType}", $"JSON={data}" });
                    break;
            }
            return Task.CompletedTask;
        }


        /// <summary>
        ///  Constrcuts the string to send to the socket.
        /// </summary>
        /// <param name="action">The action. (Alternatively for subscription 'subscribe')</param>
        /// <param name="msg">Message</param>
        /// <param name="user">Username</param>
        /// <param name="msgid">Message Identifer</param>
        /// <returns>String - JSON Object</returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="Exception"></exception>
        private string MessageConstructor(string action, string msg="", string user="", string msgid="") { // Text, MessageID, Username are the only 3 parameers you'll ever need.
            switch (action) {
                case "subscribe":
                    return new {
                        command = action,
                        identifier = new { channel = "GatewayChannel" }.Stringify()
                    }.Stringify();

                case "send_message":
                    return new {
                        command = "message",
                        identifier = new { channel = "GatewayChannel" }.Stringify(),
                        data = new {
                            action,
                            text = msg,
                            channelId
                        }.Stringify()
                    }.Stringify();

                case "send_whisper":
                    return new {
                        command = "message",
                        identifier = new { channel = "GatewayChannel" }.Stringify(),
                        data = new {
                            action,
                            username = user,
                            text = msg,
                            channelId
                        }.Stringify()
                    }.Stringify();

                // these are going to be the hardest to impl, except for unmute user.
                case "delete_message":
                    throw new NotImplementedException();
                case "mute_user":
                    throw new NotImplementedException();
                case "unmute_user":
                    throw new NotImplementedException();
                case "block_user":
                    throw new NotImplementedException();
                default:
                    throw new BotException($"{this.name}:MessageConstructor" ,$"Invalid data type fall-thru. Data: {action}");
            }
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
                    var msgsfs = MessageConstructor(action, msg, user, msgid);
                    //Print("SendMessage-JSON", $"{msgsfs}", PrintSeverity.Debug);
                    //CancellationTokenSource __cts__ = new();
                    await socket.SendAsync(Encoding.UTF8.GetBytes(msgsfs), WebSocketMessageType.Text, true, default); //byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    //__cts__.Dispose();
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


        private async Task WebsocketReaderV2() {
            string name = $"{this.name}:ReaderV2";
            Print(name, $"Starting WebSocket Reader Version 2. (Thrad: {Environment.CurrentManagedThreadId})", PrintSeverity.Debug);

            try {
                if (socket == null) throw new BotException(name, $"Thrown: Socket was null.");
                await socket.ConnectAsync(_wss_endpoint, default);
                _connected = true;
                _faulted = true;
                Interlocked.Exchange(ref _connecting, 0);

                byte[] buffer = new byte[2048];
                Print(name, $"?{IsCancelled} ?{_cancelled} :: !{!IsCancelled} !{_cancelled}", PrintSeverity.Warn);
                while(!IsCancelled) {
                    /* I rely on the host here to break the await with a {"ping"} to stop the reader. I know it's not the best but I'll work on it */
                    var socketMsg = await socket.ReceiveAsync(buffer, default);

                    if (socketMsg.MessageType == WebSocketMessageType.Text) {
                        _ = onMessage(Encoding.UTF8.GetString(buffer, 0, socketMsg.Count));
                        // REMOVE
                        //Print(name, $"Hello", PrintSeverity.Debug);

                        continue;
                    } else if (socketMsg.MessageType == WebSocketMessageType.Close) {
                        switch ((int?)socketMsg.CloseStatus) { case 1000 or 1002 or 1007 or 1008: _faulted = false; break; }
                        Print(name, $"The socket to {WSS_HOST} was terminated. (State: {(int?)socketMsg.CloseStatus ?? 1006})", PrintSeverity.Warn);
                        break;
                    }
                }
                Print(name, $"?{IsCancelled} ?{_cancelled} :: !{!IsCancelled} !{_cancelled}", PrintSeverity.Warn);
                if (IsCancelled) { // normal closure you stupid shit.
                    if (socket.State == WebSocketState.Open) { Print(name, $"Sent goodbye message to the socket.", PrintSeverity.Debug); await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default); }
                    _faulted = false;
                    Print(name, $"Socket to {WSS_HOST} closed. (Normal Closure)", PrintSeverity.Normal);
                }

            } catch (WebSocketException) {
                new BotException(name, $"Connection to {WSS_HOST} was lost. (State: {(int?)socket!.CloseStatus ?? 1006}, Thread: {Environment.CurrentManagedThreadId})");
                Interlocked.Exchange(ref _connecting, 0);
            } catch (Exception ex) {
                new BotException(name, $"Unhandled Exception (Thread: {Environment.CurrentManagedThreadId})", ex);
                Interlocked.Exchange(ref _connecting, 0);
            } finally {
                if (!IsCancelled && !_faulted) Print(name, $"Abnormal closure detected. (State: {(int?)socket!.CloseStatus ?? 1006})", PrintSeverity.Debug);
                
                _connected = false;
                
                if (_faulted) {
                    Print(name, $"Socket fault detected. Reconnection will be attempted to restore the connection.", PrintSeverity.Debug);
                    name = null;

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
