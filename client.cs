using System;
//using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text.Json;
using System.Collections.Generic;

namespace Joystick_tv__Bot
{
    class client
    {
        /*
         * THIS IS FOR CONTINUITY OF NAMING SCHEME FOR METHODS I WILL CREATE.
         * >>>>>>> DELETE AFTER COMPLETION <<<<<<<<<<<
         * 
         * JS Ref:
         * chatMessages
         * chatMessageIds
         * maxChatMessages
         * isConnectedToChat
         * deviceStatus (Toys)
         * lovenseScriptLoaded (Toys)
         * initialChatMessages
         * chatChannelEventReceived
         * streamEventChannelReceived
         * whisper_chat_channel
         * 
         * methods:
         * connectToChatChannel
         * connectToWhisperChatChannel
         * disconnectFromChatChannel
         * disconnectFromWhisperChatChannel
         * connectToStreamEventChannel
         * disconnectFromStreamEventChannel
         * disconnectFromToyExtension
         * connectToCamExtension => api.lovense (Toy)
         * 
         * sendCableMessage => channel: streamer_chat_room
         * 
         * chatChannelEventReceived =>:
         * new_message
         * event_bot_message
         * bot_message
         * visibility = public
         * receiveMessage(e.author.username, e.text), e.type
         * user.username
         * author.username
         * event_tokens_sent
         * delete_message
         * user_muted
         * user_blocked
         * 
         * streamEventChannelReceived
         * whisperChatReceived
         * updateDeviceStatus
         * updateDeviceSettings
         * playNewChatSound
         * speakMessage
         * reloadChatMessages
         * 
         * StreamerSettingsForModerator
         * 
         * StreamConfigurationTipGoal
         * tipGoalEmpty
         * activeMilestones
         * insertMilestone
         * updateTipGoal
         * deleteTipGoal
         * 
         * FAw icons used
         * streamerBadge: '<span class="pr-1" title="Streamer"><i class="fas fa-badge-check" alt="Streamer Badge" class="badge"></i></span>',
            moderatorBadge: '<span class="pr-1" title="Moderator"><i class="fas fa-shield" alt="Moderator Badge" class="badge"></i></span>',
            subscriptionBadge: '<span class="pr-1" title="Subscriber"><i class="fas fa-badge" alt="Subscriber Badge" class="badge"></i></span>',
            staffBadge: '<span class="pr-1" title="Joystick Staff"><i class="fas fa-gamepad" alt="Staff Badge" class="badge"></i></span>'

         * Dump of full methods untrimmed
         *  methods: {
        connectToChatChannel: function () {
          this.$cable._channels.subscriptions.streamer_chat_room || (this.$apollo.queries.initialChatMessages.start(), this.$cable.subscribe({
            channel: 'ChatChannel',
            stream_id: this.streamer.slug,
            user_id: this.user && this.user.id
          }, 'streamer_chat_room'), this.connectToWhisperChatChannel())
        },
        connectToWhisperChatChannel: function () {
          this.loggedIn && this.$cable.subscribe({
            channel: 'WhisperChatChannel',
            user_id: this.user.slug,
            stream_id: this.streamer.slug
          }, 'whisper_chat_channel')
        },
        disconnectFromChatChannel: function () {
          this.$cable._channels.subscriptions.streamer_chat_room && this.$cable.unsubscribe('streamer_chat_room')
        },
        disconnectFromWhisperChatChannel: function () {
          this.$cable.unsubscribe('whisper_chat_channel')
        },
        connectToStreamEventChannel: function () {
          this.$cable._channels.subscriptions.streamer_event_channel || this.$cable.subscribe({
            channel: 'EventLogChannel',
            stream_id: this.streamer.slug
          }, 'streamer_event_channel')
        },
        disconnectFromStreamEventChannel: function () {
          this.$cable._channels.subscriptions.streamer_event_channel && this.$cable.unsubscribe('streamer_event_channel')
        },
        disconnectFromToyExtension: function () {
          if (this.loggedIn && this.user && this.streamer && this.user.slug == this.streamer.slug) {
            this.lovenseScriptLoaded = !1;
            var e = document.querySelector('script#lovense-ext');
            e && (this.camExtension = null, document.head.removeChild(e))
          }
        },
         */


        /* 
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
         * 
         * 
         */

        private Uri _uri { get; set; }
        private string _header { get; set; }
        public bool _connected { get; private set; }
        public events _events;
        private ClientWebSocket _webSocket;


        /// <summary>
        /// Constructs the WebSocket client and Events.MessageConstructor
        /// </summary>
        /// <param name="uri">The WSS endpoint</param>
        /// <param name="header">Custom Headers</param>
        /// <param name="bot_id">Bot Username</param>
        /// <param name="bot_uuid">Bot UUID</param>
        /// <param name="bot_token">Bot Token</param>
        /// <param name="stream_id">Stream usernname</param>
        public client(Uri uri, string header, string bot_id, string bot_uuid, string bot_token, string stream_id="")
        {
            _webSocket = new ClientWebSocket();
            _webSocket.Options.AddSubProtocol("wss");
            _webSocket.Options.AddSubProtocol("actioncable-v1-json");
            _webSocket.Options.AddSubProtocol("actioncable-unsupported");

            _webSocket.Options.SetRequestHeader("Cookie", token.Cookie);
            _webSocket.Options.SetRequestHeader("Origin", "https://joystick.tv");
            //_webSocket.Options.SetRequestHeader("Sec-WebSocket-Key", token.SecKey);
            //_webSocket.Options.SetRequestHeader("Sec-WebSocket-Version", "13");
            //_webSocket.Options.SetRequestHeader("Upgrade", "websocket");
            //_webSocket.Options.SetRequestHeader("", "");
            //_webSocket.Options.SetRequestHeader("", "");
            //_webSocket.Options.SetRequestHeader("", "");

            _uri = uri;
            _header = header;
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

        public async Task Disconnect()
        {
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            _connected = false;
        }

        public async Task Subscribe(string _event, bool debug = false, CancellationToken cancellationToken = default)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to WebSocket endpoint.");

            if (debug)
            {
                //var test4 = "{\"command\":\"subscribe\",\"identifier\":\"{ \"channel\":\"ApplicationChannel\"}\"}";
                //var test6 = "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}";
                //string[] testy = { "{\"command\":\"subscribe\",\"identifier\":\"{ \"channel\":\"ApplicationChannel\"}\"}", "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}" };
                //string[] testy = { "{\"command\":\"subscribe\",\"identifier\":\"{ \"channel\":\"ApplicationChannel\"}\"}", "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}" };
                string[] testy = { "{\"command\":\"subscribe\",\"identifier\":\"{ \"channel\":\"ApplicationChannel\"}\"}", "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}" };
                //string[] testy = { "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"ChatChannel\",\"stream_id\":\"adachi91\",\"user_id\":\"7b33f519-b785-42ee-b2e0-5b7007149f79\"}\" }", "{\"command\":\"subscribe\",\"identifier\":\"{ \"channel\":\"ApplicationChannel\"}\"}", "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}" };
                //string[] testy = { @"{""command"":""subscribe"",""identifier"":""{""channel"":""ApplicationChannel""}""}""", "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}" };
                ArraySegment<byte> buffer;

                for(int i = 0; i<2-1; i++)
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

            foreach(var messy in msg)
            {
                var json = JsonSerializer.Serialize(messy);
                Console.WriteLine("[Client]: {0}", json);
                var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
                try {
                    await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, cancellationToken);
                } catch (Exception ex) {
                    Console.WriteLine("Shit exploded in the Subscribe Method - {0}", ex);
                }
            }
        }

        public async Task Unsubscribe(string channel)
        {
            var message = new { action = "unsubscribe", channel };
            var json = JsonSerializer.Serialize(message);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
            await _webSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task Listen(CancellationToken cancellationToken = default)
        {
            if (!_connected)
                throw new InvalidOperationException("Not connected to WebSocket endpoint.");

            var buffer = new byte[1024];
            while (_webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine("[Socket]: {0}", message);
                    if (message.Contains("{\"type\":\"welcome\"}"))
                    {
                        Task.Run(() => Subscribe("connect", false, cancellationToken));
                        Task.Run(() => Subscribe("subscribe", false, cancellationToken));
                    }
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
