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
        [Required] private Uri _wss_endpoint { get; set; }
        private bool _connected { get; set; } = false;
        private bool _faulted { get; set; } = false;
        //private long _runtime { get; set; } = 0;//this was wrote with the intention of resetting the connection after say several days to clear memory usage, as this class consumes 90% of the programs resources
        private int _reconnections { get; set; } = 0;
        private long _TTLR { get; set; } = 0; //Time To Last Reconnect
        private string channelId { get; set; } = string.Empty;
        //private string GatewayChannel { get; } = JsonSerializer.Serialize(new { channel = "GatewayChannel" });

        private Dictionary<int, string> chatHistory = new Dictionary<int, string>(); //For message deletion, muting, and blocking (severe)
        private List<string> chatHistory2 = new List<string>();

        private SemaphoreSlim messageSemaphore = new SemaphoreSlim(1, 1); //see sendMessage() method for exampliation

        private ClientWebSocket socket;
        private CancellationTokenSource Cancellation;

        //modules need to be instantiated here for access, maybe there is reason for constructor to pass which modules to load.
        private VNyan? vCat;


        /// <summary>
        /// Constructs the WebSocket client
        /// </summary>
        /// <param name="_channel_id">String - channel ID from the JWT class</param>
        /// <param name="Modules">Experimental - Setup Modules for use by the WebSocket Reader</param>
        public WebsocketClient(string _channel_id, string Modules = "vnyan,") {
            channelId = _channel_id;
            socket = null!;

            _wss_endpoint = new Uri($"{WSS_HOST}?token={Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"))}");

            foreach (var Module in Modules.Split(',')) { // mock-up for module loading.
                if (string.IsNullOrEmpty(Module)) continue;

                switch (Module.ToLower()) {
                    case "vnyan": vCat = new VNyan(); break;
                    default: break;
                }
            }

            Cancellation = new CancellationTokenSource();
            if (DEBUGGING_ENABLED) Print($"{this.name}:_constructor", $"Constructed the WebSocket Client.", PrintSeverity.Debug);
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
        public async Task<bool> Connect(bool reconnect=false) {
            if (_connected) { Print(this.name, $"Socket already in use.", PrintSeverity.Warn); return false; }

            if (!reconnect) {
                if (Connectivity.NoPing()) { Print(this.name, $"Unable to detect internet connectivity.", PrintSeverity.Error); return false; }
            }

            if (Cancellation != null && Cancellation.IsCancellationRequested && !reconnect) Cancellation.TryReset();
            if (Cancellation == null) Cancellation = new CancellationTokenSource();


            if (DEBUGGING_ENABLED)
                Print(this.name, $"Attempting to open a new Websocket Connection to {HOST}.", PrintSeverity.Debug);

            if (socket != null) {
                if(DEBUGGING_ENABLED) Print(this.name, $"Disposing an old WebSocket before continuing.", PrintSeverity.Debug);
                socket.Dispose(); // Socket re-use if `Stop()` and `Starting()`
            }

            socket = new ClientWebSocket();
            socket.Options.AddSubProtocol("actioncable-v1-json");


            _ = WebsocketReader();

            if (await socketStatus()) {
                if (DEBUGGING_ENABLED) Print(this.name, $"Sending 'subscribe' to WebSocket endpoint.", PrintSeverity.Debug);
                return await sendMessage("subscribe"); // this is a lot more pretty I like it
            }
            return false;
        }


        /// <summary>
        ///  Prevent mass flood of Connect attempts by slowing down the flow each error.
        /// </summary>
        private async Task Reconnect() {
            if (_faulted && Connectivity.NoPing()) {
                if (DEBUGGING_ENABLED) Print($"{this.name}:Reconnect", $"Could not detect internet connection, waiting for connectivity before attempting reconnection.", PrintSeverity.Debug);
                if (!await WaitForConnectivityAsync())
                    return;
                if (DEBUGGING_ENABLED) Print($"{this.name}:Reconnect", $"Connectivity re-established.", PrintSeverity.Debug);
            }

            if (!_faulted) return;

            if(Cancellation == null) { // I think this will reset the token, because when it reachs here it will be disposed.
                Cancellation = new CancellationTokenSource();
            }

            if (GetUnixTimestamp() - _TTLR > 300 || _TTLR == 0) { _TTLR = GetUnixTimestamp(); _reconnections = 0; } //reset "Time To Last Reconnect" and attempts after 5 minutes

            while (!Cancellation.IsCancellationRequested) {
                if (_reconnections < 15) _reconnections++;
                Print(this.name, $"Attempting to re-establish connection with {WSS_HOST}. n({_reconnections})", PrintSeverity.Normal);
                await Task.Delay(1_000 * _reconnections); //1, 2, 3, 4, 5 seconds

                //_ = await Connect();
                if (await Connect())
                    break;
            }
        }


        private async Task<bool> WaitForConnectivityAsync() {
            while(!Cancellation.IsCancellationRequested) {
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
            Cancellation.Cancel();

            if (!await socketStatus(-1)) {
                new BotException(this.name, "The socket did not close within the expected time (Timeout)");
                return false;
            }

            Print(this.name, $"Bot stopped succesfully.", PrintSeverity.Normal);

            return true;
        }


        private bool closure_status => (
               socket.State == WebSocketState.Closed
            || socket.State == WebSocketState.Aborted
            || socket.State == WebSocketState.CloseReceived
        );


        /// <summary>
        ///  Returns the status of the socket
        /// </summary>
        /// <returns>Bool - True if available for usage, otherwise False</returns>
        private async Task<bool> socketStatus(int code = 0) { //-1 is awaiting closure so if it's <0 it's a CLOSURE check. OTHERWISE figure it the fuck out.
            long socketWait = GetUnixTimestamp(); // Reintroduced, it's ironic, very much.
            if (DEBUGGING_ENABLED) Print(this.name, $"Attempting to assert socket status.", PrintSeverity.Debug);

            while (GetUnixTimestamp() - socketWait < 2) {
                if (socket == null) {
                    if (DEBUGGING_ENABLED) Print(this.name, $"Socket Status is null.", PrintSeverity.Debug);
                    return false;
                }

                // Check for closure
                if (code < 0 && closure_status) {
                    if (DEBUGGING_ENABLED) Print(this.name, $"Socket Status is Waiting for Closure.", PrintSeverity.Debug);
                    return true;
                }

                // Check for open state
                if (socket.State == WebSocketState.Open && code >= 0) {
                    if (DEBUGGING_ENABLED) Print(this.name, $"Socket Status is Open.", PrintSeverity.Debug);
                    return true;
                }

                await Task.Delay(5);
            }
            if (DEBUGGING_ENABLED) Print(this.name, $"Socket Status Timed-out.", PrintSeverity.Debug);
            return false;
        }


        /// <summary>
        ///  Returns the socket status
        /// </summary>
        /// <returns>Bool - True if connected and not closing, Otherwise False</returns>
        public bool Open => (_connected && !Cancellation.IsCancellationRequested);


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
            try {
                streamEvent = JsonSerializer.Deserialize<RootStreamEvents>(payload);
                //if (streamEvent.message.metadataObject.tipMenuItem == "Remove Bra") vCat.Redeem("tta");
            } catch (Exception ex) {
                new BotException($"{this.name}:StreamEvent", $"Could not deserialize the WebSocket message.", ex);
                return Task.CompletedTask; // Task failed successfully! /s
            }
            if (streamEvent == null || streamEvent.message == null) { new BotException($"{this.name}:StreamEvent", $"The chances of this falling through are so fucking rare, congrats. somehow streamevent was null :: Raw payload: {payload}"); return Task.CompletedTask; }

            switch (streamEvent.message.type) {
                case "Started":
                    // stream started
                    break;
                case "StreamEnding": // Stream ending (pending state? maybe for reconnection attempt?)

                    break;
                case "Ended": // Stream has ended

                    break;
                case "ViewerCountUpdated": // Polled maybe? otherwise on actual change. it looks like it can actually generate 2 different ID's and fire them both
                    // When viewer count changes.
                    /// noop
                    break;
                case "Tipped":
                    // Received Tip
                    /// ===> This goes to Module eventually for now create a class maybe or something to handle WebSocket connect to vNyan
                    /// This is also going to be the most tricky one to handle because you need to handle all client modules
                    /// assuming it is a 'Module' type tip.
                    var redeem = streamEvent.message.text.ToLower();
                    var redeemed = streamEvent.message.metadataObject.tipMenuItem;
                    var redeemer = streamEvent.message.metadataObject.who;
                    var cost = streamEvent.message.metadataObject.howMuch;

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
                                _ = SendMessage("send_message", $"Thank you for the tip {streamEvent.message.metadataObject.who} ! \u1F49C");
                            break;
                    }
                    break;
                case "WheelSpinClaimed":
                    // Wheelspin tip - I do not believe you have implemnted any way of handling this yet, soo. DRAW THE FUCKING OWL
                    Print("", $"{streamEvent.message.metadataObject.who} just spun the wheel and won {streamEvent.message.metadataObject.prize} for {streamEvent.message.metadataObject.howMuch} !", PrintSeverity.Normal);
                    // owl
                    break;
                case "Followed": // You haz new fren
                    _ = SendMessage("send_message", $"Welcome to the Cherry Blossoms {streamEvent.message.metadataObject.who}. Thanks so much for the Follow !");
                    Print("", $"A new follower has appeared! Say hi to {streamEvent.message.metadataObject.who}!", PrintSeverity.Normal);
                    return Task.CompletedTask;
                case "DeviceConnected": // You haz device connected and reported back by API
                    Print("", $"Your toy was registered as `{streamEvent.message.text}` from Joystick", PrintSeverity.Normal);
                    // IDK probably not worth mentioning but I don't have a toy to test how connection works. If someone was actually running Shimararu it might be useful to know on the fly when it was registered.
                    break;
                default:
                    if (DEBUGGING_ENABLED) Print($"{this.name}:StreamEvent", $"Received a new Event that is not handled! EXCITING!", PrintSeverity.Debug);
                    _ = Logger.Log($"{this.name}:WebSocket:StreamEvent", new string[] { $"Unhandled StreamEvent Raw :: ", payload });
                    break;
            }

            // Print tipped menu item if it exists
            if (!string.IsNullOrEmpty(streamEvent.message.metadataObject.tipMenuItem))
                Print("StreamEvent-Tip", $"tipMenuItem :: {streamEvent.message.metadataObject.tipMenuItem}", PrintSeverity.Warn);


            // <=================== This just fucking mistifies me, I have no fucking idea how this works. There is no lower casting. The Payload is "F"ollowed.
            // I'm just going to assume some kind of fucking weird magic is happening
            // Not compiler magic, literal fucking harry potter magic
            // I'm pretty sure I'm about to name my PC voldermonty or whatever the fuck his name is.
            // Also Snape did nothing wrong.
            // Nevermind I found the self reflection to think that I'm possibly missing something and wrong
            // I was searching metadataObj which _IS_ lower case, Type TYPE is Typecased.
            // Okay I'm leaving this in so, if you're reading this on github, I'm sorry.
            // Not really, though.
            // ======================>
            //if (streamEvent.message.metadataObject.what == "followed") { Print("Shimamura", $"A new follower has appeared! Say hi to {streamEvent.message.metadataObject.who}!", PrintSeverity.Normal); _ = SendMessage("send_message", $"Welcome to the Cherry Blossoms {streamEvent.message.metadataObject.who}. Thanks so much for the Follow !"); return Task.CompletedTask; }
            
            /// Updates the Console header to display number of viewers.
            if (streamEvent.message.type == "ViewerCountUpdated") { Console.Title = $"♥ Shimamura :: {streamEvent.message.metadataObject.numberOfViewers.ToString()} ♥"; return Task.CompletedTask; }
            //need to create a Timer class to create a new timer on timed tips e.g. Remove Bra/Mask for 30 minutes as it needs to be tracked internally to communicate with 3rd party apps like vNyan, VTS

            // Found you, you little bugger you.
            // Discover L I M P
            Print("StreamEvent", $"unhandled event: {streamEvent.message.text}", PrintSeverity.Normal); //So far: Viewer update, Stream setting update, Stream starting, Stream ending, Stream Ended
            //write the code for events on tip
            //if()
            _ = Logger.Log("StreamEvent", new string[] { streamEvent.message.text, $"who: {streamEvent.message.metadataObject.who} ::", $"what: {streamEvent.message.metadataObject.what} :: tipmenitem: {streamEvent.message.metadataObject.tipMenuItem} :: prize: {streamEvent.message.metadataObject.prize} :: howMuch: {streamEvent.message.metadataObject.howMuch}" });
            return Task.CompletedTask;
        }


        private Task onMessage_Message(string payload) { // TODO: Every one of these tries to deserialize. it's needs to be Try-Catch
            var msg = JsonSerializer.Deserialize<RootMessageEvent>(payload);
            ///chatHistory2.Add(msg.message.messageId);
            //if (chatHistory2[user_input])
            if (msg.message.visibility != "public") return Task.CompletedTask; // I think DM to bot only - not user. so this should be handled for bot-whisper interactions.

            Print($"Chat", $"{msg.message.author.username}: {msg.message.text}", PrintSeverity.Normal);
            if (!msg.message.text.StartsWith("."))
                Console.Beep();
            _ = Logger.Log("ChatMessage", new string[] { $"{msg.message.author.username}: {msg.message.text}" });
            if (msg.message.text.StartsWith(".duck")) vCat.Redeem("duck");
            else if (msg.message.text.StartsWith(".yeet")) vCat.Redeem("yeet");
            else if (msg.message.text.StartsWith(".testing")) vCat.Redeem("tta");
            return Task.CompletedTask;
        }


        private Task onMessage_PresenceEvent(string payload) {
            var presencemsg = JsonSerializer.Deserialize<RootPresenceEvent>(payload);
            var eveType = presencemsg.message.type == "enter_stream" ? "Entered the chat" : "Left the chat";
            _ = Logger.Log("UserPresence", new string[] { $"{presencemsg.message.text} {eveType}" });
            return Task.CompletedTask;
        }


        private Task onMessage(string data) {
            if (data.StartsWith("{\"type\":\"ping\"")) return Task.CompletedTask;

            if (data.Contains("confirm_subscription")) { // TODO better comparison other than "Contains"
                Print(this.name, $"Estasblished connection to chatroom.", PrintSeverity.Normal);
                return Task.CompletedTask;
            } else if (data.Contains("reject_subscription")) {
                Print(this.name, $"Could not connect to chat. Make sure everything is correctly configured.", PrintSeverity.Warn);
                return Task.CompletedTask;
            }

            if (!data.Contains("\"message\":")) return Task.CompletedTask;

            //ChatGPT's optimization is to put this at the top of the method because "it will reduce json parsing calls". I can't tell if it's 99% special or just doesn't understand my code
            JsonNode jsonNode = JsonNode.Parse(data);

            string eventType = (string)jsonNode["message"]!["event"]!;

            switch (eventType) {
                case "StreamEvent":
                    _ = onMessage_StreamEvent(data);
                    if(DEBUGGING_ENABLED) _ = Log("StreamEvent", new string[] { "Raw Socket Output:: ", $"{data}", " ::end" });
                    //TODO: Fix vNyan communication, it's not spawning an instance of vNyan properly to send Websocket request through, even override of tta did not work.
                    break;
                case "ChatMessage": //deserialize Root ChatMessage class
                    _ = onMessage_Message(data);
                    break;
                case "UserPresence": //deserialize Root UserPresence class
                    _ = onMessage_PresenceEvent(data);
                    break;
                default: //This shouldn't trigger but if it does capture it so I can inspect what went wrong
                    Print($"{this.name}-EventType", $"Unexpected request :: eventType: {eventType} :: Json Dump: {data}", PrintSeverity.Debug);
                    _ = Logger.Log($"{this.name}-EventType", new string[] { $"Unexpected request", $"EventType={eventType}", $"JSON={data}" });
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
            switch (action)
            {
                case "subscribe":
                    if (DEBUGGING_ENABLED) Print(this.name, $"Constructing subscription message for the socket.", PrintSeverity.Debug);

                    return new
                    {
                        command = action,
                        identifier = new { channel = "GatewayChannel" }.Stringify()
                    }.Stringify();

                case "send_message":
                    return new
                    {
                        command = "message",
                        identifier = new { channel = "GatewayChannel" }.Stringify(),
                        data = new {
                            action,
                            text = msg,
                            channelId
                        }.Stringify()
                    }.Stringify();

                case "send_whisper":
                    return new
                    {
                        command = "message",
                        identifier = new { channel = "GatewayChannel" }.Stringify(),
                        data = new
                        {
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
                    throw new Exception($"Invalid data type fall-thru. Data: {action}");
            }
        }

        public async Task<bool> SendMessage(string action, string msg) => await sendMessage(action, msg, "", ""); // Do you really need to know? ehh iffy check references
        public async Task<bool> SendWhisper(string action, string msg, string user) => await sendMessage(action, msg, user, "");
        public async Task<bool> Mute_User(string action, string msgid) => false;
        public async Task<bool> Unmute_User(string action, string user) => false;
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
                if (await socketStatus()) {
                    var msgsfs = MessageConstructor(action, msg, user, msgid);
                    //Print("SendMessage-JSON", $"{msgsfs}", PrintSeverity.Debug);
                    await socket.SendAsync(Encoding.UTF8.GetBytes(msgsfs), WebSocketMessageType.Text, true, Cancellation.Token); //byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    _success = true;
                } else
                    throw new BotException(this.name, "Socket status was not connected or unobtainable while trying to send a message.");
            } catch (WebSocketException wse) {
                new BotException(this.name, $"Could not send message: {action} :: msg: {msg} :: user: {user} :: messageId: {msgid}", wse);
            } catch (BotException) {
                // Ignore self thrown exception : exception.
            } catch (Exception ex) {
                new BotException(this.name, $"Unhandled Exception", ex);
            } finally { //https://stackoverflow.com/a/10260233
                messageSemaphore.Release();
            }

             return _success;
        }


        /// <summary>
        ///  Websocket Reader - Connects if not connected and waits for socket messages 
        /// </summary>
        /// <returns></returns>
        private async Task WebsocketReader() {
            if (DEBUGGING_ENABLED) Print(this.name, $"Starting the WebSocket Reader.", PrintSeverity.Debug);

            try {
                await socket.ConnectAsync(_wss_endpoint, Cancellation.Token);
                _connected = true;
                _faulted = true;

                byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
                Task<WebSocketReceiveResult> socketMsg;
                WebSocketReceiveResult socketResult;

                //Websocket Reader Loop
                while (socket.State == WebSocketState.Open && !Cancellation.IsCancellationRequested) {
                    socketMsg = socket.ReceiveAsync(buffer, default); //default is intentional - byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    var TaskTriggered = await Task.WhenAny(Task.Delay(Timeout.Infinite, Cancellation.Token), socketMsg); // Wait with a GOTO #ID, waiting to jump to either Timeout or SocketMsgReceived.

                    if (TaskTriggered != socketMsg) break;

                    socketResult = await socketMsg;

                    if (socketResult.MessageType == WebSocketMessageType.Text) {
                        _ = onMessage(Encoding.UTF8.GetString(buffer, 0, socketResult.Count));
                        continue;
                    } else if (socketResult.MessageType == WebSocketMessageType.Close) {
                        switch ((int?)socketResult.CloseStatus) { case 1000 or 1002 or 1007 or 1008: _faulted = false; break; }
                        Print(this.name, $"The socket to {WSS_HOST} was terminated. (State: {(int?)socketResult.CloseStatus ?? 1006})", PrintSeverity.Warn);
                        break;
                    } else {
                        if (socket.State != WebSocketState.Open) break;
                    }
                }

                /// This is a Normal closure block.
                if (Cancellation.IsCancellationRequested) {
                    if (socket.State == WebSocketState.Open) { if (DEBUGGING_ENABLED) Print($"{this.name}:Reader", $"Sent goodbye message to the socket.", PrintSeverity.Debug); await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default); }
                    _faulted = false;
                    if (DEBUGGING_ENABLED) Print($"{this.name}:Reader", $"Socket to {WSS_HOST} closed. (Normal Closure)", PrintSeverity.Debug);
                    Print(this.name, $"Socket to {WSS_HOST} successfully closed.", PrintSeverity.Normal);
                }
            } catch (System.Net.WebSockets.WebSocketException wsp) {
                new BotException($"{this.name}:Reader", $"Connection to {WSS_HOST} was unexpectedly reset.", wsp);
                //Print(this.name, $"WebsocketException - General Failure. Connection to {WSS_HOST} was reset", PrintSeverity.Error); //a massive loop was thrown into chaos here. I do not understand how it got into a Loop {} but once it was handled in Logger it stopped. Loop{} in ClientWebsocket class? it was a `System.Net.Sockets.SocketException` that snowballed it.
            } catch (Exception ex) {
                new BotException($"{this.name}:Reader", "Unhandled Exception", ex);
            } finally {
                if(!Cancellation.IsCancellationRequested && !_faulted) {
                    if (DEBUGGING_ENABLED) Print($"{this.name}:Reader", $"Abnormal closure detected. (State: {(int?)socket.CloseStatus ?? 1006})", PrintSeverity.Debug);
                }
                _connected = false;
                Cancellation.Cancel();

                await Task.Delay(369);
                if (_faulted) {
                    if (DEBUGGING_ENABLED) Print($"{this.name}:Reader", $"Socket fault detected. Reconnection will be attempted to restore the connection.", PrintSeverity.Debug);
                    _ = Reconnect();
                }
            }
        }

        #region JSONClass

        #region Presence
        public class PresenceMessage
        {
            public string id { get; set; }
            public string @event { get; set; }
            public string type { get; set; }
            public string text { get; set; }
            public string channelId { get; set; }
            public DateTime createdAt { get; set; }
        }

        public class RootPresenceEvent
        {
            public string identifier { get; set; }
            public PresenceMessage message { get; set; }
        }
        #endregion

        #region StreamEvents
        public class RootStreamEvents
        {
            public string identifier { get; set; }
            public Message message { get; set; }
        }

        public class Message
        {
            public string id { get; set; }
            public string @event { get; set; }
            public string type { get; set; }
            public string text { get; set; }
            public string metadata { get; set; }
            public DateTime createdAt { get; set; }
            public string channelId { get; set; }

            /// <summary>
            /// [JsonIgnore] WHAT THE FUCK 
            /// </summary>
            public MetadataObject metadataObject => JsonSerializer.Deserialize<MetadataObject>(metadata);
        }

        public class MetadataObject
        {
            public string who { get; set; }
            public string what { get; set; }
            [JsonPropertyName("how_much")]
            public int? howMuch { get; set; }
            [JsonPropertyName("tip_menu_item")]
            public string tipMenuItem { get; set; }
            public string prize { get; set; }
            [JsonPropertyName("number_of_viewers")]
            public int? numberOfViewers { get; set; }
        }
        #endregion

        #region MessageEvent
        public class RootMessageEvent
        {
            [JsonPropertyName("identifier")]
            public string identifier { get; set; }

            [JsonPropertyName("message")]
            public ChatMessage message { get; set; }
        }

        public class ChatMessage
        {
            [JsonPropertyName("event")]
            public string @event { get; set; }

            [JsonPropertyName("createdAt")]
            public DateTime createdAt { get; set; }

            [JsonPropertyName("messageId")]
            public string messageId { get; set; }

            [JsonPropertyName("type")]
            public string type { get; set; }

            [JsonPropertyName("visibility")]
            public string visibility { get; set; }

            [JsonPropertyName("text")]
            public string text { get; set; }

            [JsonPropertyName("botCommand")]
            public string botCommand { get; set; }

            [JsonPropertyName("botCommandArg")]
            public string botCommandArg { get; set; }

            [JsonPropertyName("emotesUsed")]
            public List<object> emotesUsed { get; set; }

            [JsonPropertyName("author")]
            public ChatUser author { get; set; }

            [JsonPropertyName("streamer")]
            public ChatUser streamer { get; set; }

            [JsonPropertyName("channelId")]
            public string channelId { get; set; }

            [JsonPropertyName("mention")]
            public bool mention { get; set; }

            [JsonPropertyName("mentionedUsername")]
            public string mentionedUsername { get; set; }
        }

        public class ChatUser
        {
            [JsonPropertyName("slug")]
            public string slug { get; set; }

            [JsonPropertyName("username")]
            public string username { get; set; }

            [JsonPropertyName("usernameColor")]
            public object usernameColor { get; set; }

            [JsonPropertyName("displayNameWithFlair")]
            public string displayNameWithFlair { get; set; }

            [JsonPropertyName("signedPhotoUrl")]
            public string signedPhotoUrl { get; set; }

            [JsonPropertyName("signedPhotoThumbUrl")]
            public string signedPhotoThumbUrl { get; set; }

            [JsonPropertyName("isStreamer")]
            public bool isStreamer { get; set; }

            [JsonPropertyName("isModerator")]
            public bool isModerator { get; set; }

            [JsonPropertyName("isSubscriber")]
            public bool isSubscriber { get; set; }
        }
        #endregion

        #endregion
    }
}
