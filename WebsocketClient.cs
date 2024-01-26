using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Encodings.Web;

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

        //private void Print(string msg, int lvl) => events.Print(msg, lvl);
        private Uri _uri { get; set; }
        public bool _connected { get; private set; }
        public bool isExiting { get; set; }
        /// <summary>
        /// Creates an instance of the events class to construct messages, supplying the bot id, uuid, and currently stream_id (ew)
        /// </summary>
        public events _events;
        private ClientWebSocket _webSocket;
        private bool debugging = true;


        /// <summary>
        /// Constructs the WebSocket client and Events.MessageConstructor
        /// </summary>
        /// <param name="uri">The WSS endpoint</param>
        /// <param name="bot_id">Bot Username</param>
        /// <param name="bot_uuid">Bot UUID</param>
        /// <param name="bot_token">Bot Token</param>
        /// <param name="stream_id">Stream usernname</param>
        public WebsocketClient(Uri uri, string bot_id, string bot_uuid, string bot_token, string stream_id="")
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.AddSubProtocol("wss");
            _webSocket.Options.AddSubProtocol("actioncable-v1-json");
            _webSocket.Options.AddSubProtocol("actioncable-unsupported");

            _uri = uri;
            _events = new events(bot_id, bot_uuid, bot_token, stream_id);
        }

        public async Task Connect(CancellationToken cancellationToken = default)
        {
            try {
                await _webSocket.ConnectAsync(_uri, cancellationToken);
                _connected = true;
            } catch (Exception ex) {
                Console.WriteLine("Connection Error: {0}", ex);
            }
        }

        /// <summary>
        /// async task to dispatch CloseAsync on WSSClient
        /// </summary>
        /// <returns>nothing</returns>
        public async Task Disconnect()
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            while(_webSocket.State != WebSocketState.Closed)
                Thread.Sleep(100);

            _connected = false;
            Console.WriteLine("[Socket]: Connection shutdown gracefully. Please come again.");
        }



        public object Parser(string msg)
        {
            Object obj = new object { };
            //have fun asshole.

            return obj;
        }









        public async Task parseNewFollow()
        {
            /*
             *[Socket]: {"identifier":"{\"channel\":\"ChatChannel\",\"stream_id\":\"adachi91\",\"user_id\":\"7b33f519-b785-42ee-b2e0-5b7007149f79\"}","message":{"event":"ChatMessage","createdAt":"2023-05-01T18:52:55Z","messageId":"2fae1949-9d6e-4f5b-a5ae-1e50f6c448fd","type":"new_message","visibility":"public","text":"thanks for the event","botCommand":null,"botCommandArg":null,"emotesUsed":[],"author":{"slug":"adachi91","username":"Adachi91","usernameColor":null,"displayNameWithFlair":"{{{streamerBadge}}} Adachi91","signedPhotoUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6.png?validfrom=1682966875&validto=1685559775&&hash=cpJ7pfkfeyRr2ZJL4u5GHpLl4bE%3D","signedPhotoThumbUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6-250x250.png?validfrom=1682966875&validto=1685559775&&hash=yqP1pHq2ssEmYFhj5QjA38ZFZy0%3D"},"strea
[Socket]: mer":{"slug":"adachi91","username":"Adachi91","usernameColor":null,"signedPhotoUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6.png?validfrom=1682966875&validto=1685559775&&hash=cpJ7pfkfeyRr2ZJL4u5GHpLl4bE%3D","signedPhotoThumbUrl":"https://images.joystick.tv/content/videos/joystick/production/6fe7/58b0/1937/7d7d/f77d/6fe758b019377d7df77de64802df5ca6/6fe758b019377d7df77de64802df5ca6-250x250.png?validfrom=1682966875&validto=1685559775&&hash=yqP1pHq2ssEmYFhj5QjA38ZFZy0%3D"},"chatChannel":"adachi91","mention":false,"mentionedUsername":null}}
[Socket]: {"identifier":"{\"channel\":\"EventLogChannel\",\"stream_id\":\"adachi91\"}","message":{"event":"StreamEvent","id":"b5fa6c52-882a-4f23-a79c-a40b3a0bd0de","type":"ChatMessageReceived","text":"new_message","metadata":"{}","createdAt":"2023-05-01T18:52:55Z","updatedAt":"2023-05-01T18:52:55Z"}} 
             */
        }

        public async Task parseChatChannelMessage(string _sockMessage)
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

            dynamic jsonMsg = JsonSerializer.Deserialize<dynamic>(_sockMessage);

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
                        await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
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
                if(debugging)
                    Console.WriteLine("[Client]: {0}", json);
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
                try {
                    await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
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
                if (debugging)
                    Console.WriteLine("[Client]: {0}", json);
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

                try {
                    await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                } catch (Exception ex) {
                    Console.WriteLine($"Shit exploded in the Unsubscribe method, idk kill it or something :: {ex}");
                }
            }
            Console.WriteLine("Jobs done, zug zug");
        }

        public async Task Listen(CancellationToken cancellationToken = default)
        {
            if (!_connected)
                throw new InvalidOperationException("Listener: The websocket was not open.");

            byte[] buffer = new byte[4096]; //1024 bytes IF the header Sec-Websocket-Maximum-Message-Size is detected, then that is the maximum size the buffer can be to prevent DDoSing.
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    //if (!message.Contains("ping"))
                        Console.WriteLine("[Socket]: {0}\r\n", message);

                    if (message.Contains("{\"type\":\"welcome\"}"))
                    {
                        //Task.Run(() => Subscribe("connect", false, cancellationToken));
                        //Task.Run(() => Subscribe("subscribe", false, cancellationToken));
                    }
                } else if (result.MessageType == WebSocketMessageType.Close) { 
                    
                } else {
                    Console.WriteLine("Invalid WebSocketMessageType: {0}", result.MessageType.ToString());
                }
            }

            if (_webSocket.State != WebSocketState.Open)
                Console.WriteLine("[Socket]: The connection was forcefully closed. [{0}]", _webSocket.State.ToString());
            _connected = false;
        }
    }
}
