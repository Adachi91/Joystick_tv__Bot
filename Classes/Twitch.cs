using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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

        public async Task<bool> ConnectAsync() {
            var name = $"{this.name}:TcpClient";

            if (DEBUGGING_ENABLED) Print(name, $"Attempting to connect to {_irc_endpoint}:6697.", PrintSeverity.Debug);

            try {
                await _client.ConnectAsync(_irc_endpoint, 6697);
                return true;
            }
            catch (BotException) { /* recursive prevention */ }
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
            catch (BotException) { }
            catch (Exception ex) {
                new BotException(this.name, "Unable to manipulate client state.", ex);
            }
            return false;
        }

        private async Task<bool> StartListeningAsync() {

            await Task.Delay(1);

            return true;
        }

        public async void blah() {
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


        //public class WebSocket {
            //wheatwat do they even use websockets topkek
        //}
    }
}
