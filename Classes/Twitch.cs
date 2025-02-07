using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes {
    internal class Twitch {
        private readonly string name = "Twitch";
        private const string _irc_endpoint = "irc.chat.twitch.tv";
        private string _username;
        private string _twitch_token;
        private string _channel_name;
        /// <summary>
        ///  WHATEVER THE THING - OH yeah,
        ///  <============ Scopes ============>
        ///  channel:bot
        ///  channel:manage:broadcast
        ///  channel:read:polls
        ///  channel:manage:polls
        ///  user:bot
        ///  user:edit
        ///  user:read:chat
        ///  user:manage:whispers
        ///  user:write:chat
        ///  clips:edit
        ///  moderator:manage:banned_users
        ///  moderator:read:blocked_terms
        ///  moderator:read:chat_messages
        ///  moderator:manage:chat_messages
        ///  moderator:read:chat_settings
        ///  moderator:read:chatters
        ///  moderator:read:shoutouts
        ///  moderator:manage:shoutouts
        ///  
        /// IRC:
        /// chat:edit
        /// chat:read
        /// </summary>

        private TcpClient _client { get; set; }

        /// <summary>
        ///  Construct a new Twitch instance to connect to the IRC chat.
        /// </summary>
        /// <param name="Username"><see cref="string"/> Streamer's Username</param>
        /// <param name="Token"><see cref="string"/> AcessToken<para>This requires an OAuth flow I believe see <see cref="OAuthClient"/></para></param>
        /// <param name="channel"><see cref="string"/> Streamer's Channel</param>
        public Twitch(string Username, string Token, string channel) {
            _client = new TcpClient();

            _username = "";
            _twitch_token = "";
            _channel_name = "Adachi91";
        }

        class Config {
            record OAuth(string host, string client_id, string client_secret, string authorize_uri, string token_uri, string redirect_uri, string scope, string response_type = "code", AuthTypes auth = AuthTypes.None, Service service = Service.Twitch);
            record WebSocketConfig(string endpoint, string token, string channelId, Service service = Service.Twitch); // WiP
        }

        public async Task<bool> ConnectAsync() {
            var name = $"{this.name}:TcpClient";

            if (DEBUGGING_ENABLED) Print(name, $"Attempting to connect to {_irc_endpoint}:6697.", PrintSeverity.Debug);

            try {
                await _client.ConnectAsync(_irc_endpoint, 6697);
                return true;
            }
            catch (SocketException sEx) { new BotException(name, $"A socket exception has occured while connecting to {_irc_endpoint}", sEx); }
            catch (Exception ex) { new BotException(name, $"Could not connect to {_irc_endpoint}.", ex); }

            return false;
        }

        public async Task<bool> CloseAsync() {
            try {
                _client.Close();
                while(_client.Connected) {
                    await Task.Delay(30);
                }
                Print(this.name, $"The connect to {_irc_endpoint} has closed successfulewlj", PrintSeverity.Debug);
                return true;
            }
            catch (Exception ex) {
                new BotException(this.name, "Unable to manipulate client state.", ex);
            }
            return false;
        }

        private async Task<bool> StartListeningAsync() {

            await Task.Delay(1);

            return true;
        }

        public async Task blah() {
            using (var client = new TcpClient()) {
                await client.ConnectAsync(_irc_endpoint, 6697);
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true }) {
                    // Authenticate
                    await writer.WriteLineAsync($"PASS {_twitch_token}");
                    await writer.WriteLineAsync($"NICK {_username}");

                    // Join channel
                    await writer.WriteLineAsync($"JOIN #{_channel_name}");

                    Console.WriteLine($"Connected to Twitch chat for #{_channel_name}");

                    // Listen for messages
                    while (true) {
                        if (stream.DataAvailable) {
                            var message = await reader.ReadLineAsync();
                            if (message != null) {
                                // Respond to PING to keep connection alive
                                if (message.StartsWith("PING")) {
                                    await writer.WriteLineAsync("PONG :tmi.twitch.tv");
                                    continue;
                                }

                                // Parse chat messages
                                if (message.Contains("PRIVMSG")) {
                                    var split = message.Split(new[] { "!" }, 2, StringSplitOptions.None);
                                    var username = split[0].Substring(1); // Extract username
                                    var chatMessage = message.Split(new[] { "PRIVMSG" }, 2, StringSplitOptions.None)[1]
                                        .Split(new[] { ':' }, 2, StringSplitOptions.None)[1]; // Extract message

                                    Console.WriteLine($"{username}: {chatMessage}");
                                }
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        ///  Strong-typed name class.<br />
        ///  Deserialize here.
        ///  
        /// <para>Usage TwitchMessage T -Fucking brackets.</para>
        /// </summary>
        public class WebSocket {
            #region WebSocket_Responses
            public class TwitchMessage<T> {
                [JsonPropertyName("metadata")]
                public Metadata Metadata { get; set; } = new();

                [JsonPropertyName("payload")]
                public T Payload { get; set; } = default!;
            }

            public class Metadata {
                [JsonPropertyName("message_id")]
                public string MessageId { get; set; } = string.Empty;

                [JsonPropertyName("message_type")]
                public string MessageType { get; set; } = string.Empty;

                [JsonPropertyName("message_timestamp")]
                public DateTime MessageTimestamp { get; set; }
            }

            public class SessionWelcome {
                [JsonPropertyName("session")]
                public SessionInfo Session { get; set; } = new();
            }

            public class SessionInfo {
                [JsonPropertyName("id")]
                public string Id { get; set; } = string.Empty;

                [JsonPropertyName("status")]
                public string Status { get; set; } = string.Empty;

                [JsonPropertyName("keepalive_timeout_seconds")]
                public int KeepAliveTimeoutSeconds { get; set; }
            }

            public class SessionKeepAlive { }

            public class SessionReconnect {
                [JsonPropertyName("session")]
                public ReconnectInfo Session { get; set; } = new();
            }

            public class ReconnectInfo {
                [JsonPropertyName("id")]
                public string Id { get; set; } = string.Empty;

                [JsonPropertyName("status")]
                public string Status { get; set; } = string.Empty;
            }

            public class SubscriptionEvent<T> {
                [JsonPropertyName("subscription")]
                public SubscriptionInfo Subscription { get; set; } = new();

                [JsonPropertyName("event")]
                public T Event { get; set; } = default!;
            }

            public class SubscriptionInfo {
                [JsonPropertyName("id")]
                public string Id { get; set; } = string.Empty;

                [JsonPropertyName("status")]
                public string Status { get; set; } = string.Empty;

                [JsonPropertyName("type")]
                public string Type { get; set; } = string.Empty;

                [JsonPropertyName("version")]
                public string Version { get; set; } = string.Empty;
            }

            public class ChannelFollow {
                [JsonPropertyName("user_id")]
                public string UserId { get; set; } = string.Empty;

                [JsonPropertyName("user_name")]
                public string UserName { get; set; } = string.Empty;

                [JsonPropertyName("broadcaster_user_id")]
                public string BroadcasterUserId { get; set; } = string.Empty;
            }

            public class ChannelSubscribe {
                [JsonPropertyName("user_id")]
                public string UserId { get; set; } = string.Empty;

                [JsonPropertyName("user_name")]
                public string UserName { get; set; } = string.Empty;

                [JsonPropertyName("broadcaster_user_id")]
                public string BroadcasterUserId { get; set; } = string.Empty;
            }

            public class ChannelCheer {
                [JsonPropertyName("user_id")]
                public string UserId { get; set; } = string.Empty;

                [JsonPropertyName("user_name")]
                public string UserName { get; set; } = string.Empty;

                [JsonPropertyName("bits")]
                public int Bits { get; set; }
            }

            public class ChannelRaid {
                [JsonPropertyName("from_broadcaster_user_id")]
                public string FromBroadcasterUserId { get; set; } = string.Empty;

                [JsonPropertyName("to_broadcaster_user_id")]
                public string ToBroadcasterUserId { get; set; } = string.Empty;

                [JsonPropertyName("viewers")]
                public int Viewers { get; set; }
            }
            #endregion
        }
    }
}
