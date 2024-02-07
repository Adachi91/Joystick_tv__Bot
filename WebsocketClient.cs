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

        public bool _connected { get; private set; }
        public bool isExiting { get; set; }
        /// <summary>
        /// Creates an instance of the events class to construct messages, supplying the bot id, uuid, and currently stream_id (ew)
        /// </summary>
        //public events _events;
        private ClientWebSocket WSSClient;

        public Dictionary<DateTime, Tuple<string, string>> History = new Dictionary<DateTime, Tuple<string, string>>();
        private bool saveHistory { get; set; }

        private CancellationTokenSource cts = new CancellationTokenSource();
        public CancellationToken ctx;


        /// <summary>
        /// Constructs the WebSocket client and Events.MessageConstructor
        /// </summary>
        public WebsocketClient(bool _history = false)
        {
            if (_history) saveHistory = true;

            ctx = cts.Token;

            WSSClient = new ClientWebSocket();
            //WSSClient.Options.AddSubProtocol("wss");
            WSSClient.Options.AddSubProtocol("actioncable-v1-json");
            
            //WSSClient.Options.AddSubProtocol("actioncable-unsupported");


            //_events = new events(bot_id, bot_uuid, bot_token, stream_id);
        }


        public async Task Connect()
        {
            if(WSSClient.State == WebSocketState.Open) { Print($"[WSSClient]: The socket is already open", 2); return; }
            Print($"[WSSClient]: Attempting to connect to {WSS_HOST}", 0);
            try {
                WSSClient.Options.SetRequestHeader("Sec-WebSocket-Protocol", "actioncable-v1-json");
                await WSSClient.ConnectAsync(new Uri(WSS_GATEWAY), ctx);
                _connected = true;
            } catch (Exception ex) {
                Print($"[WSSClient]: Connection Error {ex}", 3);
            }
        }


        //
        public void Close() {
            cts.Cancel();
            //Disconnect();
        }


        /// <summary>
        /// async task to dispatch CloseAsync on WSSClient
        /// </summary>
        /// <returns>nothing</returns>
        private async Task Disconnect()
        {
            _connected = false;

            if (WSSClient.State == WebSocketState.Closed || WSSClient.State == WebSocketState.Aborted || WSSClient.State == WebSocketState.CloseSent) {
                Print($"[WSSClient]: The socket is already closed to {WSS_HOST}", 3);
                return;
            }

            await WSSClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "Farewell", ctx);
            while(WSSClient.State != WebSocketState.Closed)
                Thread.Sleep(100);

            Print($"[WSSClient]: Socket shutdown gracefully. Please come again.", 1);
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

        //
        private void whatifIMessageQueued()
        {

        }

        //what I'mma use chatdumbpg34 when I can
        private void IterateJsonElement(JsonElement element)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                string key = property.Name;
                JsonElement value = property.Value;

                Print($"[JSON]: Key: {key}", 0);

                if (value.ValueKind == JsonValueKind.Object)
                    IterateJsonElement(value);
                else if (value.ValueKind == JsonValueKind.Array)
                {
                    //Console.WriteLine("Array found:");
                    foreach (var arrayElement in value.EnumerateArray()) //fuck arrays
                    {
                        // Handle or iterate the array elements
                        // If they are objects, you can call IterateJsonElement(arrayElement)
                    }
                } else {
                    // It's a simple value (string, number, etc.)
                    Print($"[JSON]: Value: {value}", 0);
                }
            }
        }

        public void onMessage(string data) {
            //var msg = JsonSerializer.Deserialize<JSONDataStruct>(data);

            /*dynamic msg = JsonSerializer.Deserialize<dynamic>(data);

            Print($"[WSS.onMessage]: {msg.identifier} {msg.message}", 0);

            dynamic msg2 = JsonSerializer.Deserialize<dynamic>(msg.message);

            Print($"[WSS.onMessage]: {msg2.type} --- {msg2}", 0);*/

            ///lord jesus help me sweet baby jesus
            ///
            //Print($"[WSSClient]: SAFETY DUMP: \n{data}\n\n", 0);

            if(data.Contains("ping")) { /*Print($"[WSSClient]: noop", 0);*/ return;  }

            if(data.Contains("confirm_subscription"))
            {
                Print($"[JSON]: I'm in.\nWhat? I couldn't resist, jesus give me a break.", 0);
                return;
            } else if(data.Contains("reject_subscription")) {
                Print($"[JSON]: reject_subscription", 0);
                return;
            }

            if (!data.Contains("\"message\":")) return;

            JsonNode jsonNode = JsonNode.Parse(data); //message.event
            //JsonNode Msgf = JsonNode.Parse((string)jsonNode!["message"]!);

            string eventType = (string)jsonNode["message"]!["event"]!;
            Print($"[JSON]: Plwese work: {eventType}",0);

            switch (eventType) //dejavu I've been here beforoeoro iterating over an object and digging in deeper and deeper and deeppppppppppppppperrrrrrrrrrrrrrrrrrrrrrrrr 
            {
                case "StreamEvent":
                    var streamEvent = JsonSerializer.Deserialize<RootStreamEvents>(data);
                    Print($"[StreamEvent]: idk shit happened :: {streamEvent.message.text}", 1);
                    break;
                case "ChatMessage":
                    var msg = JsonSerializer.Deserialize<RootMessageEvent>(data);
                    if (msg.message.visibility != "public") return;

                    Print($"[Chat]: {msg.message.author.username}: {msg.message.text}", 1);
                    break;
                case "UserPresence":
                    var presencemsg = JsonSerializer.Deserialize<RootPresenceEvent>(data);
                    var eveType = presencemsg.message.type == "enter_stream" ? "Entered the chat" : "Left the chat";
                    Print($"[Presence]: {presencemsg.message.text} {eveType}!", 1);
                    break;
                default:
                    Print($"[JSON]: ehh hi", 2);
                    break;
            }
            return;

            dynamic typeOfData = JsonSerializer.Deserialize<dynamic>(data);
            IterateJsonElement(typeOfData);
            return;
            if (typeOfData is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object) {
                foreach (JsonProperty f in jsonElement.EnumerateObject())
                {
                    Print($"[JSON]: k: {f.Name}, v: {f.Value}\n", 0); //now to fix this shit
                    if(f.Name == "message")
                    {
                        var moo = "";
                        
                    }
                }
            }
            //^ that's going to explode or print some shit like system.net.json.object.penis

            //try catch JSONBinding or some shit

            string values = typeOfData.message.@event; //might be better to try 'type' not 'event' //yoink 470a4687924f9561b55f990c6e624800c7108109e84fc88e0598d641e36b7e9f

            switch (values)
            {
                case "StreamEvent":
                    Print($"[JSON]: Received StreamEvent - \n{data}\n", 0);
                    break;
                case "ChatMessage":
                    Print($"[JSON]: Received ChatMessage - \n{data}\n", 0);
                    break;
                case "confirm_subscription" or "reject_subscription":
                    Print($"[JSON]: Received This won't happen - \n{data}\n", 0);
                    break;
                case "UserPresence":
                    Print($"[JSON]: Received UserPresence - \n{data}\n", 0);
                    break;
                default:
                    Print($"[JSON]: ehh hi", 0);
                    break;
            }

            if(data.Contains("StreamEvent"))
            {
                //deserialz chatmessage
            }
            else if(data.Contains("ChatMessage"))
            {
                //dersialz streamevent
            } else if(data.Contains(""))
            {
                //try dynamic
            }

            //Print($"[WSSClient]: I died. {data}", 0);
        }

        public void FidgetyStuff(string msg) {
            switch (msg.ToLower())
            {
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
            Print($"[JSON]: You are preparing to send this shit: \n\n{fuckoff}\n\n", 0);
            //JObject o = new JObject {{ "command", data },{ "identifier", fuckoff }};

            //Print($"{o.ToString()}", 0);

            //JArray jsonarray = new JArray();


            //var a = JSONobjToString(GATEWAY_IDENTIFIER);


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

            int timeout = 10;
            while (WSSClient.State != WebSocketState.Open) { Thread.Sleep(500); timeout--; if (timeout <= 0) { Print($"YOU DONE FUCKED UP TIMEOUT BITCH", 4); break; } }
            //Print($"\n\n\n\n", 0);
            if (WSSClient.State == WebSocketState.Open)
                WSSClient.SendAsync(buffer, WebSocketMessageType.Text, true, ctx);
            else
                Print($"[WSSClient]: RACE CONDITION, TIME TO DIE MOTHER FUCKERS", 4);
        }


        public async Task Listen(CancellationToken ctx) //todo: optimize
        {
            //if (!_connected) { Print($"[WSSClient]: Could not listen to socket, as it is not open.", 3); return; }
            while(WSSClient.State != WebSocketState.Open) { Thread.Sleep(11); } 
            Print($"[WSSClient]: WSS connection to {WSS_HOST} Successful. Now listening...", 0);

            byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
            Task<WebSocketReceiveResult> resultTask;
            WebSocketReceiveResult result;
            while (WSSClient.State == WebSocketState.Open && !ctx.IsCancellationRequested)
            {
                try {
                    var cancelreceiver = Task.Delay(Timeout.Infinite, ctx);
                    resultTask = WSSClient.ReceiveAsync(new ArraySegment<byte>(buffer), default); // yay I was right//I think it's aborting on cancel receive but I'm too lazy to google.
                    var complete = await Task.WhenAny(cancelreceiver, resultTask);

                    if (complete == cancelreceiver) break;
                    
                    result = await resultTask;

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        onMessage(message);
                        continue;

                        if (message.Contains("{\"type\":\"welcome\"}"))
                        {
                            //Task.Run(() => Subscribe("connect", false, cancellationToken));
                            //Task.Run(() => Subscribe("subscribe", false, cancellationToken));
                        }
                    } else if (result.MessageType == WebSocketMessageType.Close) {
                        Print($"[WSSClient]: Remote {WSS_HOST} closed the socket with Websocket Code: {(int)result.CloseStatus}", 1);
                        return;
                    } else {
                        Print($"[WSSClient]: Unhandled Exception {result.MessageType.ToString()}", 3);
                        if (WSSClient.State == WebSocketState.Closed) return;
                    }
                }
                catch (TaskCanceledException)
                {
                    Print($"[WSSClient]: Task was cancelled.", 1);
                    break; // Exit the loop if the task was cancelled
                }
                catch (Exception ex)
                {
                    // Handle other exceptions
                    Print($"[WSSClient]: Exception: {ex.Message}", 3); //it's scary that it knew that 3 was Error code. and 1 is normal status I've never shown it my print code, so deduced it
                }
            }

            if(ctx.IsCancellationRequested && WSSClient.State == WebSocketState.Open) {
                Print($"[WSSClient]: Closed connection to {WSS_HOST}", 1);
                Disconnect();
                return;
            }

            if (!ctx.IsCancellationRequested && WSSClient.State != WebSocketState.Open)
                Print($"[WSSClient]: Socket closed remotely from {WSS_HOST}", 3);

            Print($"[WSSClient]: Bye.", 4);
            Disconnect(); //if it's not already closed properly
        }


































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
