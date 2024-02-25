using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Collections.Generic;

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

        private bool _connected { get; set; } = false;
        private bool _closing { get; set; } = false;
        private bool _faulted { get; set; } = false;
        private bool _userhalted { get; set; } = false;
        private long _runtime { get; set; } = 0;//this was wrote with the intention of resetting the connection after say several days to clear memory usage, as this class consumes 90% of the programs resources
        private int _reconnections { get; set; } = 0;
        private long _TTLR { get; set; } = 0; //Time To Last Reconnect
        private long _lastping { get; set; } = 0;
        private string channelId { get; set; } = string.Empty;
        private string GatewayChannel { get; } = JsonSerializer.Serialize(GATEWAY_IDENTIFIER);

        private Dictionary<int, string> chatHistory = new Dictionary<int, string>(); //For message deletion, muting, and blocking (severe)
        private List<string> chatHistory2 = new List<string>();

        private SemaphoreSlim messageSemaphore = new SemaphoreSlim(1, 1); //see sendMessage() method for exampliation

        private ClientWebSocket socket = null;
        private CancellationTokenSource cts;
        private CancellationToken ctx;

        //modules need to be instantiated here for access, maybe there is reason for constructor to pass which modules to load.
        private VNyan vCat = new VNyan();


        /// <summary>
        /// Constructs the WebSocket client
        /// </summary>
        public WebsocketClient() {
            if (!string.IsNullOrEmpty(CHANNELGUID))
                channelId = CHANNELGUID;
        }


        /// <summary>
        ///  Starts the Websocket Client and connects to WSS_HOST
        /// </summary>
        /// <returns></returns>
        public async Task Connect() {
            if (_connected) { Print($"[Websocket]: Socket already in use", 2); return; }
            //if(_faulted) { Print($"[Websocket]: Attempting to reconnect to {WSS_HOST}", 1); _faulted = false; }
            if (socket != null) { socket.Dispose(); }

            socket = new ClientWebSocket();
            socket.Options.AddSubProtocol("actioncable-v1-json");


            _ = WebsocketReader();

            if(await socketStatus()) {
                _ = sendMessage("subscribe", new string[] { "", "", "" });
            } else {
                Print("[Websocket]: Timeout sending Subscribe message to socket.", 3);
                _ = Close();
            }
        }


        /// <summary>
        ///  Prevent mass flood of Connect attempts by slowing down the flow each error.
        /// </summary>
        /// <returns></returns>
        private async Task Reconnect() {
            if (!_faulted) return;

            Print($"[Websocket]: Socket faulted. Attempting to re-establish connection with {WSS_HOST}. n({_reconnections})", 1);

            if (GetUnixTimestamp() - _TTLR > 300) { _TTLR = GetUnixTimestamp(); _reconnections = 0; } //reset "Time To Last Reconnect" and attempts after 5 minutes

            await Task.Delay((1500 * _reconnections)); //instant, 1, 2, 3, 4 ,5 seconds
            if (_reconnections < 20) _reconnections++;

            _ = Connect();
        }


        /// <summary>
        ///  Gracefully closes the Websocket Client
        /// </summary>
        /// <param name="code">(Optional) negative integre if external closure</param>
        public async Task Close(int code = 0) {
            _closing = true;
            cts.Cancel();

            if (await socketStatus(-1))
                throw new BotException("Websocket", "The socket did not close within the expected time (Timeout)"); //TODO: Fix this and find the nearest catcher, or refactor.

            if (code < 0)
                Print($"[Shimamura]: Stopped successful", 1);
        }


        /// <summary>
        ///  Returns the status of the socket
        /// </summary>
        /// <returns>Bool - True if available for usage, otherwise False</returns>
        private async Task<bool> socketStatus(int code = 0) {
            long socketWait = GetUnixTimestamp();

            while(true) {
                if (socket != null)
                {
                    if (socket.State == WebSocketState.Open && code < 0 && (GetUnixTimestamp() - socketWait > 7))
                        return true;

                    if (socket.State == WebSocketState.Open && code >= 0)
                        return true;
                    
                    if (GetUnixTimestamp() - socketWait > 7)
                        return false;
                } else
                    if (GetUnixTimestamp() - socketWait > 7)
                        return false;

                await Task.Delay(100);
            }
        }


        /// <summary>
        ///  Returns the socket status
        /// </summary>
        /// <returns>Bool - True if connected and not closing, Otherwise False</returns>
        public bool Open() {
            return (_connected && !_closing);
        }


        public string[] getMessage(int id)
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


        private async Task<bool> onMessage_StreamEvent(string payload) {
            var streamEvent = JsonSerializer.Deserialize<RootStreamEvents>(payload);
            Print($"[StreamEvent]: idk happened :: {streamEvent.message.text}", 1); //So far: Viewer update, Stream setting update, Stream starting, Stream ending, Stream Ended
            if (streamEvent.message.metadataObject.tipMenuItem == "Remove Bra") vCat.Redeem("tta");
            if (!string.IsNullOrEmpty(streamEvent.message.metadataObject.tipMenuItem)) Print($"[StreamEvent]: !! tipMenuItem :: {streamEvent.message.metadataObject.tipMenuItem}", 2);

            //need to create a Timer class to create a new timer on timed tips e.g. Remove Bra/Mask for 30 minutes as it needs to be tracked internally to communicate with 3rd party apps like vNyan, VTS

            //write the code for events on tip
            await WriteToFileShrug("StreamEvent", new string[] { streamEvent.message.createdAt.ToString(), streamEvent.message.text, $"who: {streamEvent.message.metadataObject.who} ::", $"what: {streamEvent.message.metadataObject.what} :: tipmenitem: {streamEvent.message.metadataObject.tipMenuItem} :: prize: {streamEvent.message.metadataObject.prize} :: howMuch: {streamEvent.message.metadataObject.howMuch}" });
            return true;
        }


        private async Task onMessage_Message(string payload) {
            var msg = JsonSerializer.Deserialize<RootMessageEvent>(payload);
            ///chatHistory2.Add(msg.message.messageId);
            //if (chatHistory2[user_input])
            if (msg.message.visibility != "public") return;
            //hardcoded compensation until modules is finished // I believe a redeem did not successfully go through and I'm hardcoding a free redeem.
            if (msg.message.text.Contains(".redeem") && msg.message.author.username == "murphymichael902") vCat.Redeem("tta");

            Print($"[Chat]: {msg.message.author.username}: {msg.message.text}", 1);
            Console.Beep();
            await WriteToFileShrug("ChatMessage", new string[] { msg.message.createdAt.ToString(), $"{ msg.message.author.username}: {msg.message.text}" });
            if (msg.message.text.StartsWith(".duck")) vCat.Redeem("duck");
            else if (msg.message.text.StartsWith(".yeet")) vCat.Redeem("yeet");
            else if (msg.message.text.StartsWith(".testing")) vCat.Redeem("tta");
        }


        private async Task onMessage_PresenceEvent(string payload) {
            var presencemsg = JsonSerializer.Deserialize<RootPresenceEvent>(payload);
            var eveType = presencemsg.message.type == "enter_stream" ? "Entered the chat" : "Left the chat";
            await WriteToFileShrug("UserPresence", new string[] { presencemsg.message.createdAt.ToString(), $"{presencemsg.message.text} {eveType}" });
        }


        private async Task onMessage(string data) {
            if (data.StartsWith("{\"type\":\"ping\"")) return;

            if (data.Contains("confirm_subscription")) {
                Print($"[Shimamura]: Connected to chat!", 1);
                return;
            } else if (data.Contains("reject_subscription")) {
                Print($"[Shimamura]: Could not connect to chat. Make sure everything is correctly configured.", 2);
                return;
            }

            if (!data.Contains("\"message\":")) return;

            //ChatGPT's optimization is to put this at the top of the method because "it will reduce json parsing calls". I can't tell if it's 99% special or just doesn't understand my code
            JsonNode jsonNode = JsonNode.Parse(data);

            string eventType = (string)jsonNode["message"]!["event"]!;
            if(string.IsNullOrEmpty(channelId)) { channelId = (string)jsonNode["message"]!["channelId"]!; updateKey("CHANNELGUID", channelId); }

            switch (eventType) {
                case "StreamEvent":
                    await onMessage_StreamEvent(data);
                    break;
                case "ChatMessage": //deserialize Root ChatMessage class
                    await onMessage_Message(data);
                    break;
                case "UserPresence": //deserialize Root UserPresence class
                    await onMessage_PresenceEvent(data);
                    break;
                default: //This shouldn't trigger but if it does capture it so I can inspect what went wrong
                    Print($"[JSONParser]: There was an unexpected request :: eventType: {eventType}, Json Dump: {data}", 3);
                    break;
            }
        }


        /// <summary>
        ///  Constrcuts the string to send to the socket.
        /// </summary>
        /// <param name="action">The action. (Alternatively for subscription 'subscribe')</param>
        /// <param name="dparam">0:text, 1:username, 2:messageId</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        /// <exception cref="Exception"></exception>
        private string MessageConstructor(string action, params string[] dparam) {
            //TODO: Finish message constructor for all sendmessage datatypes (subscribe/send_message/etc...)

            //leaving this for reference, I was made aware that using anonymous types at compile time create classes automagically.
            /*var test = new JsonObject
            {
                ["command"] = "message",
                ["identifier"] = "{\"channel\":\"GatewayChannel\"}",//JsonSerializer.Serialize(GATEWAY_IDENTIFIER),
                ["data"] = JsonSerializer.Serialize(new JsonObject { ["action"] = "send_message", ["text"] = action, ["channelId"] = "470a4687924f9561b55f990c6e624800c7108109e84fc88e0598d641e36b7e9f" })
            };*/
            if (string.IsNullOrEmpty(channelId) && action != "subscribe") throw new BotException("Websocket", "The channelId has not yet been acquired please type init in your Joystick.tv chatroom first. This only needs to be done once.");

            switch (action)
            {
                case "subscribe":
                    return JsonSerializer.Serialize(new
                    {
                        command = action,
                        identifier = GatewayChannel
                    });

                case "send_message":
                    return JsonSerializer.Serialize(new
                    {
                        command = "message",
                        identifier = GatewayChannel,
                        data = JsonSerializer.Serialize(new {
                            action,
                            text = dparam[0],
                            channelId
                        })
                    });

                case "send_whisper":
                    return JsonSerializer.Serialize(new
                    {
                        command = "message",
                        identifier = GatewayChannel,
                        data = JsonSerializer.Serialize(new
                        {
                            action,
                            username = dparam[1],
                            text = dparam[0],
                            channelId
                        })
                    });

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


        /// <summary>
        ///  Websocket Writer - Send message to the socket and wait for success or failure
        /// </summary>
        /// <param name="action">The action. (Alternatively for subscription 'subscribe')</param>
        /// <param name="dparams">0:text, 1:username, 2:messageId</param>
        /// <returns>Bool - True if sent, False with error</returns>
        public async Task<bool> sendMessage(string action, params string[] dparams) {
            bool _success = false;

            //https://www.codetinkerer.com/2018/06/05/aspnet-core-websockets.html
            //Attempting to invoke any other operations in parallel may corrupt the instance.
            //Attempting to invoke a send operation while another is in progress or a receive operation while another is in progress will result in an exception.
            await messageSemaphore.WaitAsync();
            try {
                if (await socketStatus()) {
                    var msgsfs = MessageConstructor(action, dparams);
                    Print($"[JSON]: {msgsfs}", 0);
                    await socket.SendAsync(Encoding.UTF8.GetBytes(msgsfs), WebSocketMessageType.Text, true, ctx); //byte[] can be implicitly converted to ArraySegment<byte> without explicitly wrapping new ArraySegment<byte>, not really documented
                    _success = true;
                } else
                    throw new BotException("Websocket", "Socket did not open in a timely manner");
            } catch (WebSocketException wse) {
                new BotException("Websocket", $"Could not send message: {action} :: text: {dparams[0]} :: username: {dparams[1]} :: messageId: {dparams[2]}");
                Print($"[Websocket]: The exception was :: {wse}", 0);
            } catch (Exception ex) {
                new BotException("Websocket", $"Unhandled exception :: ", ex); //TODO: recursive fix BotException(,BotException())
            } finally {
                messageSemaphore.Release();
                //if (socket.State != WebSocketState.Open)
                    //Print($"[Websocket]: The socket was closed because of the message. Closing status: {(int)socket.CloseStatus}", 2);
            }

             return _success;
        }


        /// <summary>
        ///  Websocket Reader - Connects if not connected and waits for socket messages 
        /// </summary>
        /// <returns></returns>
        private async Task WebsocketReader() {
            cts = new CancellationTokenSource();
            ctx = cts.Token;

            try {
                await socket.ConnectAsync(new Uri(WSS_GATEWAY), ctx);
                _connected = true;
                _closing = false;
                _faulted = true;

                byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
                Task<WebSocketReceiveResult> socketMsg;
                WebSocketReceiveResult socketResult;

                //Websocket Reader Loop
                while (socket.State == WebSocketState.Open && !ctx.IsCancellationRequested) {
                    socketMsg = socket.ReceiveAsync(new ArraySegment<byte>(buffer), default); //default is intentional
                    var TaskTriggered = await Task.WhenAny(Task.Delay(Timeout.Infinite, ctx), socketMsg);

                    if (TaskTriggered != socketMsg) break;

                    socketResult = await socketMsg;

                    if (socketResult.MessageType == WebSocketMessageType.Text) {
                        _ = onMessage(Encoding.UTF8.GetString(buffer, 0, socketResult.Count));
                        continue;
                    } else if (socketResult.MessageType == WebSocketMessageType.Close) {
                        Print($"[Websocket]: {WSS_HOST} closed the socket with Code: {(int)socketResult.CloseStatus}", 1);
                        if ((int)socketResult.CloseStatus == 1007 || (int)socketResult.CloseStatus == 1002) _faulted = false;
                        break;
                    } else {
                        if (socket.State != WebSocketState.Open) break;
                    }
                }

                if (ctx.IsCancellationRequested && socket.State == WebSocketState.Open) { //1000
                    _faulted = false;
                    Print($"[Websocket]: Closing socket to {WSS_HOST}...", 0);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default);
                    Print($"[Websocket]: Socket successfully closed", 1);
                }
            } catch (System.Net.WebSockets.WebSocketException) {
                Print($"[Websocket]: WebsocketException - General Failure. Connection to {WSS_HOST} was reset", 3); //a massive loop was thrown into chaos here. I do not understand how it got into a Loop {} but once it was handled in Logger it stopped. Loop{} in ClientWebsocket class? it was a `System.Net.Sockets.SocketException` that snowballed it.
            } catch (Exception ex) { //WHO KNOWS?!
                Print($"[Websocket]: Unhandle exception :: {ex}", 3);
            } finally {
                _connected = false;
                _closing = true;
                cts.Cancel();

                await Task.Delay(369);
                if (_faulted) _ = Reconnect();
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

            [JsonIgnore]
            public MetadataObject metadataObject => JsonSerializer.Deserialize<MetadataObject>(metadata);
        }

        public class MetadataObject
        {
            public string who { get; set; }
            public string what { get; set; }
            public int? howMuch { get; set; }
            public string tipMenuItem { get; set; }
            public string prize { get; set; }
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
