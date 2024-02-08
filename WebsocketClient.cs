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
   the body MUST be a 2-byte unsigned integer (in network byte order)
   representing a status code with value /code/ defined in Section 7.4.
   Following the 2-byte integer, the body MAY contain UTF-8-encoded data
   with value /reason/
         * 1000 indicates a normal closure, meaning that the purpose for which the connection was established has been fulfilled.
         * 1001 indicates that an endpoint is "going away", such as a server going down or a browser having navigated away from a page.
         * 1002 indicates that an endpoint is terminating the connection due to a protocol error.
         * 1003 indicates that an endpoint is terminating the connection because it has received a type of data it cannot accept (e.g., an endpoint that understands only text data MAY send this if it receives a binary message).
         */

        private bool _connected { get; set; }
        private bool _faulted { get; set; }
        //public bool isExiting { get; set; }
        /// <summary>
        /// Creates an instance of the events class to construct messages, supplying the bot id, uuid, and currently stream_id (ew)
        /// </summary>
        //public events _events;
        private ClientWebSocket WSSClient;

        //public Dictionary<DateTime, Tuple<string, string>> History = new Dictionary<DateTime, Tuple<string, string>>();
        private bool saveHistory { get; set; }

        private CancellationTokenSource cts;
        public CancellationToken ctx;
        private VNyan vCat = new VNyan();


        /// <summary>
        /// Constructs the WebSocket client and Events.MessageConstructor
        /// </summary>
        public WebsocketClient(bool _history = false)
        {
            if (_history) saveHistory = true;
            _connected = false;
            _faulted = false;

            //WSSClient = new ClientWebSocket();
            //WSSClient.Options.AddSubProtocol("actioncable-v1-json");
        }


        /*public async Task Connect_original()
        {
            if(WSSClient.State == WebSocketState.Open) { Print($"[WSSClient]: The socket to {HOST} is already open", 2); return; }

            cts = new CancellationTokenSource();
            ctx = cts.Token;
            Print($"[WSSClient]: Attempting to connect to {WSS_HOST}", 0);
            try {
                WSSClient.Options.SetRequestHeader("Sec-WebSocket-Protocol", "actioncable-v1-json");
                await WSSClient.ConnectAsync(new Uri(WSS_GATEWAY), ctx);
                _connected = true;
            } catch (Exception ex) {
                Print($"[WSSClient]: Connection Error {ex}", 3);
            }
            using(ClientWebSocket wss = new ClientWebSocket())
            {
                wss.Options.SetRequestHeader("", "");
                await wss.ConnectAsync(new Uri(WSS_GATEWAY), ctx);
            }
        }*/


        public async Task Connect() {
            if(WSSClient != null) { WSSClient.Dispose(); }
            if(_connected) { Print($"[Websocket]: Socket already in use", 2); return; }
            if(_faulted) { Print($"[Websocket]: Attempting to reconnect to {WSS_HOST}", 1); _faulted = false; }

            WSSClient = new ClientWebSocket();
            WSSClient.Options.AddSubProtocol("actioncable-v1-json");

            Task.Run(() => { startWebsocket(); sendMessage("subscribe"); });
        }


        //
        public void Close() {
            if(!_connected) { Print($"[Websocket]: Socket is not open (Nothing Happens)", 2); return; }

            cts.Cancel();
        }


        /// <summary>
        /// async task to dispatch CloseAsync on WSSClient
        /// </summary>
        /// <returns>nothing</returns>
        private async Task Disconnect()
        {
            cts.Cancel();
            /*
            _connected = false;

            if (WSSClient.State == WebSocketState.Closed || WSSClient.State == WebSocketState.Aborted || WSSClient.State == WebSocketState.CloseSent) {
                Print($"[WSSClient]: The socket is already closed to {WSS_HOST}", 3);
                return;
            }

            await WSSClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Farewell", ctx);
            while(WSSClient.State != WebSocketState.Closed)
                Thread.Sleep(100);

            Print($"[WSSClient]: Socket shutdown gracefully. Please come again.", 1);*/
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


        public void onMessage(string data) {
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
                    WriteToFileShrug(eventType, new string[] { streamEvent.message.createdAt.ToString(), streamEvent.message.text, $"who: {streamEvent.message.metadataObject.who}::", $"what: {streamEvent.message.metadataObject.what}" });
                    break;
                case "ChatMessage": //deserialize Root ChatMessage class
                    var msg = JsonSerializer.Deserialize<RootMessageEvent>(data);
                    if (msg.message.visibility != "public") return;

                    Print($"[Chat]: {msg.message.author.username}: {msg.message.text}", 1);
                    WriteToFileShrug(eventType, new string[] { msg.message.createdAt.ToString(), $"{msg.message.author.username}: {msg.message.text}" });
                    if (msg.message.text.StartsWith(".duck")) vCat.Redeem("duck");
                    else if (msg.message.text.StartsWith(".yeet")) vCat.Redeem("yeet");
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

        public void FidgetyStuff(string msg) {
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


        public async Task sendMessage(string data) {

            //var tmp = JsonSerializer.Serialize(GATEWAY_IDENTIFIER);
            //var temp2 = $"\"{tmp.Replace("\"", "\\\"")}\"";

            //dynamic test = JObject.Parse(GATEWAY_IDENTIFIER);
            ///{ "command": "subscribe",
            ///"identifier": "{\"channel\":\"GatewayChannel\",\"streamer\":\"joystickuser\"}" }
            //user_id backup
            var c = "{\\\"channel\\\":\\\"GatewayChannel\\\",\\\"streamer\\\":\\\"adachi91\\\"}";

            var fuckoff = "{\"command\":\"" + data + "\",\"identifier\":\"" + c + "\"}";
            //Print($"[JSON]: You are preparing to send this shit: \n\n{fuckoff}\n\n", 0);
            


            object obi = new
            {
                command = data,
                identifier = "asdfsadfsadf"
            };
            //var obi = stringifyJSON("channel");


            ///var json = JsonSerializer.Serialize(obi);
            //var json = "ASDFASDFSAF";
            //Print($"[WSSClient]: Sending \n\n{json}\n\n", 0);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(fuckoff));
            //foreach (byte boot in buffer)
            //Print($"\n{boot:X2}", 0);

            int timeout = 50;
            while (WSSClient.State != WebSocketState.Open) { Thread.Sleep(100); timeout--; if (timeout <= 0) { Print($"YOU DONE FUCKED UP TIMEOUT BITCH", 4); break; } }
            //Print($"\n\n\n\n", 0);
            if (WSSClient.State == WebSocketState.Open)
                WSSClient.SendAsync(buffer, WebSocketMessageType.Text, true, ctx);
            else
                Print($"[WSSClient]: RACE CONDITION, TIME TO DIE MOTHER FUCKERS", 4);
        }


        private async Task startWebsocket() {
            cts = new CancellationTokenSource();
            ctx = cts.Token;

            //using(ClientWebSocket socket = new ClientWebSocket()) {
                try {
                    //socket.Options.SetRequestHeader("Sec-WebSocket-Protocol", "actioncable-v1-json");
                    await WSSClient.ConnectAsync(new Uri(WSS_GATEWAY), ctx);
                    _connected = true;

                    byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
                    Task<WebSocketReceiveResult> listenTask;
                    WebSocketReceiveResult listenResult;

                    while (WSSClient.State == WebSocketState.Open && !ctx.IsCancellationRequested) {
                        listenTask = WSSClient.ReceiveAsync(new ArraySegment<byte>(buffer), default); // yay I was right//I think it's aborting on cancel receive but I'm too lazy to google.
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
                            break;
                        } else {
                            Print($"[Websocket]: Unhandled Exception {listenResult.MessageType.ToString()}", 3);
                            if (WSSClient.State == WebSocketState.Closed || WSSClient.State == WebSocketState.Aborted) break;
                        }
                    }

                    if(ctx.IsCancellationRequested && WSSClient.State == WebSocketState.Open) { //normal closure by user
                        Print($"[Websocket]: Closing socket to {WSS_HOST}...", 0);
                        await WSSClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "FaretheWell", default);
                        _connected = false;
                        Print($"[Websocket]: Socket successfully closed", 1);
                    }
                    //anything broken;
                } catch (System.Net.WebSockets.WebSocketException wse) { //General Failure
                    Print($"[Websocket]: WebsocketException :: {wse}", 3);
                    _faulted = true;
                    cts.Cancel();
                }
                catch (Exception ex) { //WHO KNOWS?!
                    Print($"[Websocket]: Unhandle exception :: {ex}", 3);
                } finally {
                    if(_faulted) {
                        _connected = false;
                        cts.Cancel();
                        Print("[Websocket]: Socket faulted (CORE DUMPED)  :)", 0);
                        Connect();
                    }
                }
            //}
        }


        /* public async Task Listen(CancellationToken ctx) //todo: optimize
        {
            //if (!_connected) { Print($"[WSSClient]: Could not listen to socket, as it is not open.", 3); return; }
            while(WSSClient.State != WebSocketState.Open) { Thread.Sleep(11); } 
            Print($"[WSSClient]: WSS connection to {WSS_HOST} Successful. Now listening...", 0);

            byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
            Task<WebSocketReceiveResult> resultTask;
            WebSocketReceiveResult result;
            bool _faulted = false;
            while (WSSClient.State == WebSocketState.Open && !ctx.IsCancellationRequested)
            {
                try {
                    var cancelreceiver = Task.Delay(Timeout.Infinite, ctx);
                    resultTask = WSSClient.ReceiveAsync(new ArraySegment<byte>(buffer), default); // yay I was right//I think it's aborting on cancel receive but I'm too lazy to google.
                    var complete = await Task.WhenAny(cancelreceiver, resultTask);

                    if (complete == cancelreceiver) break;
                    
                     /// System.Net.WebSockets.WebSocketException: 'The remote party closed the WebSocket connection without completing the close handshake.'
                     /// This exception was originally thrown at this call stack:
                     /// [External Code]
                     /// ShimamuraBot.WebsocketClient.Listen(System.Threading.CancellationToken) in WebsocketClient.cs
                     
                    result = await resultTask;

                    if (result.MessageType == WebSocketMessageType.Text) {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        onMessage(message);
                        continue;
                    } else if (result.MessageType == WebSocketMessageType.Close) {
                        Print($"[WSSClient]: Remote {WSS_HOST} closed the socket with Websocket Code: {(int)result.CloseStatus}", 1);
                        return;
                    } else {
                        Print($"[WSSClient]: Unhandled Exception {result.MessageType.ToString()}", 3);
                        if (WSSClient.State == WebSocketState.Closed) return;
                    }
                } catch (TaskCanceledException) {
                    Print($"[WSSClient]: Task was cancelled.", 1);
                    break; // Exit the loop if the task was cancelled
                } catch (System.Net.WebSockets.WebSocketException wssException) {
                    if(wssException.WebSocketErrorCode == WebSocketError.Faulted) {
                        Print($"[WSSClient]: Socket closed unexpectedly", 2);
                        _faulted = true;
                        break;
                    }
                } catch (Exception ex) {
                    // Handle other exceptions
                    Print($"[WSSClient]: Exception: {ex.Message}", 3); //it's scary that it knew that 3 was Error code. and 1 is normal status I've never shown it my print code, so deduced it
                } finally {
                    if (_faulted) {
                        Print($"[WSSClient]: Attempting to reconnect to {WSS_HOST}", 1);
                        cts.Cancel();
                        Connect();
                    }
                }
            }

            if(ctx.IsCancellationRequested && WSSClient.State == WebSocketState.Open) {
                Print($"[WSSClient]: Closed connection to {WSS_HOST}", 1);
                Disconnect();
                return;
            }

            if (!ctx.IsCancellationRequested && WSSClient.State != WebSocketState.Open)
                Print($"[WSSClient]: Socket closed remotely from {WSS_HOST}", 3);

            //Print($"[WSSClient]: Bye.", 4);
            Disconnect(); //if it's not already closed properly
        } */


































        public async Task parseNewFollow()
        {
            /*
             *[Socket]: {"identifier":"{\"channel\":\"ChatChannel\",\"stream_id\":\"adachi91\",\"user_id\":\"7b33f519-b785-42ee-b2e0-5b7007149f79\"}","message":{"event":"ChatMessage","createdAt":"2023-05-01T18:52:55Z","messageId":"2fae1949-9d6e-4f5b-a5ae-1e50f6c448fd","type":"new_message","visibility":"public","text":"thanks for the event","botCommand":null,"botCommandArg":null,"emotesUsed":[],"author":{"slug":"adachi91","username":"Adachi91","usernameColor":null,"displayNameWithFlair":"{{{streamerBadge}}} Adachi91","signedPhotoUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6.png?validfrom=1682966875&validto=1685559775&&hash=cpJ7pfkfeyRr2ZJL4u5GHpLl4bE%3D","signedPhotoThumbUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6-250x250.png?validfrom=1682966875&validto=1685559775&&hash=yqP1pHq2ssEmYFhj5QjA38ZFZy0%3D"},"strea
[Socket]: mer":{"slug":"adachi91","username":"Adachi91","usernameColor":null,"signedPhotoUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6.png?validfrom=1682966875&validto=1685559775&&hash=cpJ7pfkfeyRr2ZJL4u5GHpLl4bE%3D","signedPhotoThumbUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6-250x250.png?validfrom=1682966875&validto=1685559775&&hash=yqP1pHq2ssEmYFhj5QjA38ZFZy0%3D"},"chatChannel":"adachi91","mention":false,"mentionedUsername":null}}
[Socket]: {"identifier":"{\"channel\":\"EventLogChannel\",\"stream_id\":\"adachi91\"}","message":{"event":"StreamEvent","id":"b5fa6c52-882a-4f23-a79c-a40b3a0bd0de","type":"ChatMessageReceived","text":"new_message","metadata":"{}","createdAt":"2023-05-01T18:52:55Z","updatedAt":"2023-05-01T18:52:55Z"}} 
             */
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
