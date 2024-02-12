using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Runtime.InteropServices.JavaScript;
using System.ComponentModel;

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
        private bool _faulted { get; set; } = false;
        private long _runtime { get; set; } = 0;
        private int _reconnections { get; set; } = 0;
        private long _TTLR { get; set; } = 0; //Time To Last Reconnect

        private ClientWebSocket socket;
        private CancellationTokenSource cts;
        private CancellationToken ctx;

        //modules
        private VNyan vCat = new VNyan();


        /// <summary>
        /// Constructs the WebSocket client
        /// </summary>
        public WebsocketClient() //I can't really think of a reason to keep a constructor
        {
            //_connected = false;
            //_faulted = false;
        }


        /// <summary>
        ///  Starts the Websocket Client and connects to WSS_HOST
        /// </summary>
        /// <returns></returns>
        public async Task Connect() {
            if(_connected) { Print($"[Websocket]: Socket already in use", 2); return; }
            //if(_faulted) { Print($"[Websocket]: Attempting to reconnect to {WSS_HOST}", 1); _faulted = false; }
            if(socket != null) { socket.Dispose(); }

            socket = new ClientWebSocket();
            socket.Options.AddSubProtocol("actioncable-v1-json");

            Task.Run(() => { startWebsocket(); sendMessage("subscribe"); });
        }

        /// <summary>
        ///  Prevent mass flood of Connect attempts by slowing down the flow each error.
        /// </summary>
        /// <returns></returns>
        private async Task Reconnect() {
            if (!_faulted) return;

            Print($"[Websocket]: Socket faulted. Attempting to re-establish connection with {WSS_HOST}. n({_reconnections})", 1);

            if (GetUnixTimestamp() - _TTLR > 300) { _TTLR = GetUnixTimestamp(); _reconnections = 0; } //reset "Time To Last Reconnect" and attempts after 5 minutes

            Task.Delay((1000 * _reconnections)).Wait(); //instant, 1, 2, 3, 4 ,5 seconds
            if(_reconnections < 5) _reconnections++;

            Connect();
        }

        /// <summary>
        ///  Gracefully closes the Websocket Client
        /// </summary>
        public async Task Close(int code = 0) {
            if(!_connected) { Print($"[Websocket]: Socket is not open (Nothing Happens)", 2); return; }

            cts.Cancel();

            int _timeout = 100;

            while(socket != null || socket.State != WebSocketState.Closed) {
                if (_timeout <= 0) { Print($"[Websocket]: The socket did not close within the expected time (Timeout)", 3); break; }
                await Task.Delay(100);
                _timeout--;
            }

            if (code < 0 && (socket == null || socket.State == WebSocketState.Closed))
                Print($"[Shimamura]: Succesfully shutdown!", 1);
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

        #region OutboundRootMessage
        /*public class OutboundMessageRoot
        {
            public string Command { get; set; }
            public string Identifier => JsonSerializer.Serialize(new IdentifierObject { Channel = "GatewayChannel" });

            [JsonIgnore]
            public OutboundData DataObject { get; set; }

            public string Data => JsonSerializer.Serialize(DataObject);
        }*/
        public class OutboundMessageRoot {
            public string command { get; set; }

            [JsonIgnore]
            public IdentifierObject IdentifierObject { get; set; }

            public string identifier => JsonSerializer.Serialize(IdentifierObject);//, new JsonSerializerOptions { WriteIndented = true });

            [JsonIgnore]
            public OutboundData DataObject { get; set; }

            public string data => JsonSerializer.Serialize(DataObject);//, new JsonSerializerOptions { WriteIndented = true });
        }

        public class IdentifierObject
        {
            public string channel { get; set; }
        }

        public class OutboundData {
            public string action { get; set; }
            public string text { get; set; }
            public string channelId { get; set; }
        }
        #endregion

        #endregion

        public string testMessage(string data)
        {
            var outboundmessage = new OutboundMessageRoot
            {
                command = "message",
                IdentifierObject = new IdentifierObject { channel = "GatewayChannel" },

                DataObject = new OutboundData
                {
                    action = "send_message",
                    text = data,
                    channelId = "470a4687924f9561b55f990c6e624800c7108109e84fc88e0598d641e36b7e9f"
                }
            };

            //var a = JsonSerializer.Serialize<OutboundMessageRoot>(outboundmessage);
            var a = JsonSerializer.Serialize(outboundmessage);//, new JsonSerializerOptions { WriteIndented = true });

            sendMessage("", a);
            return a;
        }

        private void onMessage(string data) {
            //ignore pings - maybe check if pings stop could be a socket issue.
            if(data.Contains("ping"))  return;

            if(data.Contains("confirm_subscription")) {
                Print($"[Shimamura]: Connected to chat!", 1);
                return;
            } else if(data.Contains("reject_subscription")) {
                Print($"[Shimamura]: Could not connect to chat. Make sure everything is correctly configured.", 1);
                return;
            }

            if (!data.Contains("\"message\":")) return;

            JsonNode jsonNode = JsonNode.Parse(data);

            string eventType = (string)jsonNode["message"]!["event"]!;

            switch (eventType)  {
                case "StreamEvent": //deserialize Root StreamEvent class
                    var streamEvent = JsonSerializer.Deserialize<RootStreamEvents>(data);
                    Print($"[StreamEvent]: idk shit happened :: {streamEvent.message.text}", 1);
                    if(streamEvent.message.metadataObject.tipMenuItem == "Remove Bra") {
                        vCat.Redeem("tta");
                    }

                    //write the code for events on tip
                    WriteToFileShrug(eventType, new string[] { streamEvent.message.createdAt.ToString(), streamEvent.message.text, $"who: {streamEvent.message.metadataObject.who}::", $"what: {streamEvent.message.metadataObject.what}" });
                    break;
                case "ChatMessage": //deserialize Root ChatMessage class
                    var msg = JsonSerializer.Deserialize<RootMessageEvent>(data);
                    if (msg.message.visibility != "public") return;

                    Print($"[Chat]: {msg.message.author.username}: {msg.message.text}", 1);
                    WriteToFileShrug(eventType, new string[] { msg.message.createdAt.ToString(), $"{msg.message.author.username}: {msg.message.text}" });
                    if (msg.message.text.StartsWith(".duck")) vCat.Redeem("duck");
                    else if (msg.message.text.StartsWith(".yeet")) vCat.Redeem("yeet");
                    else if (msg.message.text.StartsWith(".testing")) vCat.Redeem("tta");
                    break;
                case "UserPresence": //deserialize Root UserPresence class
                    var presencemsg = JsonSerializer.Deserialize<RootPresenceEvent>(data);
                    var eveType = presencemsg.message.type == "enter_stream" ? "Entered the chat" : "Left the chat";
                    WriteToFileShrug(eventType, new string[] { presencemsg.message.createdAt.ToString(), $"{presencemsg.message.text} {eveType}" });
                    //Print($"[Presence]: {presencemsg.message.text} {eveType}!", 1);
                    break;
                default: //This shouldn't trigger but if it does capture it so I can inspect what went wrong
                    Print($"[JSONParser]: There was an unexpected request :: eventType: {eventType}, Json Dump: {data}", 3);
                    break;
            }
        }

        private void FidgetyStuff(string msg) {
            switch (msg.ToLower()) {
                case "ping":
                    break;
                case "test":
                    sendMessage("APPLES");
                    break;
                case "yeet":
                    //vCat msg
                    break;
                case "duck":
                    //vCat msg
                    break;
                default:
                    break;
            }
        }

        private string stringifyJSON(string data, object obj2 = null, object obj3 = null)
        {
            var json = JsonSerializer.Serialize(obj2);
            var gateway = JsonSerializer.Serialize(GATEWAY_IDENTIFIER);

            Print($"{gateway}", 0);

            using(JsonDocument doc = JsonDocument.Parse(gateway))
            {
                var root = doc.RootElement;

                var ttem = new
                {
                    command = data, //"command":"data" //\\u2
                    identifier = root
                };

                var resp = JsonSerializer.Serialize(ttem);

                Print($"{resp}", 0);
                Environment.Exit(0);
                return resp;
            }
            return null;
        }


        private async Task sendMessage(string data, string debugdata = "") {

            ///{ "command": "subscribe",
            ///"identifier": "{\"channel\":\"GatewayChannel\",\"streamer\":\"joystickuser\"}" }
            //user_id backup
            string fuckoff;
            if (string.IsNullOrEmpty(debugdata))
            {
                var c = "{\\\"channel\\\":\\\"GatewayChannel\\\",\\\"streamer\\\":\\\"adachi91\\\"}";

                fuckoff = "{\"command\":\"" + data + "\",\"identifier\":\"" + c + "\"}";
            } else {
                fuckoff = debugdata;    
            }
            Print($"[JSON]: You are preparing to send this shit: \n\n{fuckoff}\n\n", 0);


            ///var json = JsonSerializer.Serialize(obi);
            //Print($"[WSSClient]: Sending \n\n{json}\n\n", 0);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(fuckoff));

            int timeout = 50;
            while (socket.State != WebSocketState.Open) { Thread.Sleep(100); timeout--; if (timeout <= 0) { return; } } //timeout reached no socket open. returning
            
            //if (socket.State == WebSocketState.Open)
            socket.SendAsync(buffer, WebSocketMessageType.Text, true, ctx);
        }


        private async Task startWebsocket() {
            cts = new CancellationTokenSource();
            ctx = cts.Token;

            try {
                await socket.ConnectAsync(new Uri(WSS_GATEWAY), ctx);
                _connected = true;
                _faulted = false;

                byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
                Task<WebSocketReceiveResult> listenTask;
                WebSocketReceiveResult listenResult;

                while (socket.State == WebSocketState.Open && !ctx.IsCancellationRequested) {
                    listenTask = socket.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                    var complete = await Task.WhenAny(Task.Delay(Timeout.Infinite, ctx), listenTask);

                    if (complete != listenTask) break;

                    listenResult = await listenTask;

                    if (listenResult.MessageType == WebSocketMessageType.Text) {
                        string message = Encoding.UTF8.GetString(buffer, 0, listenResult.Count);
                        onMessage(message);
                        continue;
                    } else if (listenResult.MessageType == WebSocketMessageType.Close) {
                        Print($"[Websocket]: {WSS_HOST} closed the socket with Code: {(int)listenResult.CloseStatus}", 1);
                        cts.Cancel();
                        _connected = false;
                        break;
                    } else {
                        Print($"[Websocket]: Unhandled Exception {listenResult.MessageType.ToString()}", 3);
                        if (socket.State == WebSocketState.Closed || socket.State == WebSocketState.Aborted) break;
                    }
                }

                if (ctx.IsCancellationRequested && socket.State == WebSocketState.Open) { //normal closure by user
                    Print($"[Websocket]: Closing socket to {WSS_HOST}...", 0);
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default);
                    _connected = false;
                    Print($"[Websocket]: Socket successfully closed", 1);
                }
                //anything broken;
            }
            catch (System.Net.WebSockets.WebSocketException wse) { //General Failure
                
                Print($"[Websocket]: WebsocketException - General Failure (Connectivity issue) - will attempt a reconnect", 3); //a massive loop was thrown into chaos here. I do not understand how it got into a Loop {} but once it was handled in Logger it stopped. Loop{} in ClientWebsocket class? it was a `System.Net.Sockets.SocketException` that snowballed it.
                WriteToFileShrug("Error", new string[] { DateTime.UtcNow.ToString(), $"Connection to {WSS_HOST} was Reset" }); //best terminology? technically it's partially right.
                _faulted = true;
                _connected = false;
                cts.Cancel();
                Task.Delay(369).Wait(); //trying to keep it from overloading the logger
            } catch (Exception ex) { //WHO KNOWS?!
                Print($"[Websocket]: Unhandle exception :: {ex}", 3);
            } finally {
                if (_faulted) Reconnect();
            }
        }












        /*public async Task parseChatChannelMessage(string _sockMessage)
        { //come here if channel == ChatChannel, 
            if (!_sockMessage.Contains("\"ChatChannel\""))
                return;
            /*
             * [Socket]: {
             * "identifier":"{
             *  \"channel\":\"ChatChannel\",
             *  \"stream_id\":\"adachi91\",
             *  \"user_id\":\"7b33f519-b785-42ee-b2e0-5b7007149f79\"}",
             *  "message":
             *  {
             *      "event":"ChatMessage",
             *      "createdAt":"2023-05-01T18:47:01Z",
             *      "messageId":"3ce41b14-83b1-41f1-b9ee-67415d7ae604",
             *      "type":"new_message",
             *      "visibility":"public",
             *      "text":"I've become so numb, hello bot",
             *      "botCommand":null,
             *      "botCommandArg":null,
             *      "emotesUsed":[],
             *      "author":{
             *          "slug":"adachi91",
             *          "username":"Adachi91",
             *          "usernameColor":null,
             *          "displayNameWithFlair":"{{{streamerBadge}}} Adachi91",
             *          "signedPhotoUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6.png?validfrom=1682966521&validto=1685559421&&hash=nRCHzwhWtcdxTR022L%2FNAet%2BbtA%3D",
             *          "signedPhotoThumbUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6-250x250.png?validfrom=1682966521&validto=1685559421&&hash=BONQeYylVm0DjzgHOFk4QseqO2M%3D"
             *      },
             *      "streamer":{
             *          "slug":"adachi91",
             *          "username":"Adachi91",
             *          "usernameColor":null,
             *          "signedPhotoUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6.png?validfrom=1682966521&validto=1685559421&&hash=nRCHzwhWtcdxTR022L%2FNAet%2BbtA%3D",
             *          "signedPhotoThumbUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6-250x250.png?validfrom=1682966521&validto=1685559421&&hash=BONQeYylVm0DjzgHOFk4QseqO2M%3D"
             *     },
             *     "chatChannel":"adachi91",
             *     "mention":false,
             *     "mentionedUsername":null
             * }
             *}
[Socket]: 
[Socket]: {"identifier":"{\"channel\":\"EventLogChannel\",\"stream_id\":\"adachi91\"}","message":{"event":"StreamEvent","id":"3af57a80-bfa3-4fe8-98cd-c8e8ae496a3f","type":"ChatMessageReceived","text":"new_message","metadata":"{}","createdAt":"2023-05-01T18:47:01Z","updatedAt":"2023-05-01T18:47:01Z"}}
             */

        /*    dynamic jsonMsg = JsonSerializer.Deserialize<dynamic>(_sockMessage);

            //emotes used??
            Console.WriteLine("[{0}] {1}: {2}", jsonMsg.message.createdAt, jsonMsg.message.author.username, jsonMsg.message.text);
            //[00:00:00] Adachi: Hello --Basic
            //[Adachi91][00:00:00] John: Hello --Channel
            //displayNameWithFlair {{{streamerBadge}}} ? stream_id == author.username - Is the stream Owner
        }


        private bool SubscriptionSuccess(string resp) {
            bool result = false;
            bool somethingwentwrong = false;
            //yada yada code stuff that checks if it was rejected or successful

            if (somethingwentwrong)
                throw new Exception($"I couldn't understand if the Subscription was successful or not.\r\n{resp}\r\n");

            return result;
        }

        /// <summary>
        /// Keys track of what channels the bot is subscribed to so it can ubsccruibe easier I think I just had   a stroke writing that.
        /// </summary>
        /// <param name="channel">Channel name</param>
        public void AcknowledgeSubscription(string channel, bool fuckitsjustaname = false)
        { //I want to keep this here so I can parse incoming sucessfull subscriptions and then command the list from here, instead of a fire and forget in events

            //C# 10 introduced new rules for accessing static members. Previously, it was allowed to access a static member using an instance reference, but now it's not allowed anymore.
            if (_events.Subscriptions.ContainsKey(channel))
                _events.Subscriptions[channel] = !_events.Subscriptions[channel];
            else
                _events.Subscriptions.Add(channel, fuckitsjustaname);
        }

        private void MainParser(string resp)
        { //Break down resp and send them to the correct subparser or build a gigantic pile of shit here.

            if(resp.Contains(""))
            {
                //sub ack
            } else if(resp.Contains(""))
            {
                //unsub ack
            }
        }

        /// <summary>
        /// Subscribe to a channel
        /// </summary>
        /// <param name="_event">Command</param>
        /// <param name="debug">Temp for testing bypassing all code</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task Subscribe(string _event, bool debug = false, CancellationToken cancellationToken = default) //TODO YOU HAVE TO HADD AN EXTRA ARG besides Command you need to know WHO to subscribe to, jackass.
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to WebSocket endpoint.");

            //not sure how many events there are ehhhhhhhhh I think I got most of them ? probably not.

            if (debug)
            {
                string[] testy = {
                    "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"ApplicationChannel\\\"}\"}",
                    "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"SystemEventChannel\\\",\\\"user_id\\\":\\\"" + "TODELETE" + "\\\"}\"}",
                    "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"EventLogChannel\\\",\\\"stream_id\\\":\\\"adachi91\\\"}\"}",
                    "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"ChatChannel\\\",\\\"stream_id\\\":\\\"adachi91\\\",\\\"user_id\\\":\\\"" + "TODELETE" + "\\\"}\"}",
                    "{\"command\":\"subscribe\",\"identifier\":\"{\\\"channel\\\":\\\"WhisperChatChannel\\\",\\\"user_id\\\":\\\"" + "TODELTE" + "\\\",\\\"stream_id\\\":\\\"adachi91\\\"}\"}",
                };

                ArraySegment<byte> buffer;

                for(int i = 0; i<=4; i++)
                {
                    //Console.WriteLine("-------------");
                    Console.WriteLine(">> {0}", testy[i]);
                    //Console.WriteLine("-------------");
                    buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(testy[i]));
                    try {
                        await WSSClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    } catch (Exception ex) { Console.WriteLine(ex); }
                    //Thread.Sleep(150);
                }
                return;
            }

            List<Object> msg = _events.MessageConstructor(_event);

            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            foreach (var messy in msg)
            {
                var json = JsonSerializer.Serialize(messy, options);

                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
                try {
                    await WSSClient.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
                } catch (Exception ex) {
                    Console.WriteLine($"Shit exploded in the Subscribe Method - {ex}");
                }
            }
        }

        /// <summary>
        /// Send Unsubscribe messages if leaving a channel, or disconnecting.
        /// </summary>
        /// <param name="_event">Event to send to the constructor for JSONifying</param>
        /// <param name="debug">ASDFADSFADSFASDFASDF DELETE ME</param>
        /// <param name="cancellationToken">Pizza</param>
        /// <returns>nothing</returns>
        public async Task Unsubscribe(string _event, bool debug = false, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            List<Object> msg = _events.MessageConstructor(_event);

            foreach (var THEGODDAMNMESSAGETOSENDONEATATIMEORSOMETHINGIDONTFUCKINGKNOWOK in msg)
            {
                var json = JsonSerializer.Serialize(THEGODDAMNMESSAGETOSENDONEATATIMEORSOMETHINGIDONTFUCKINGKNOWOK, options);
                
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

                try {
                    await WSSClient.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                } catch (Exception ex) {
                    Console.WriteLine($"Shit exploded in the Unsubscribe method, idk kill it or something :: {ex}");
                }
            }
            Console.WriteLine("Jobs done, zug zug");
        }*/

        
    }
}
