using ShimamuraBot.Classes.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static ShimamuraBot.WebsocketClient<T>;

namespace ShimamuraBot.Classes
{
    internal class Joystick : IDisposable {
        public WebsocketClient<Joystick.WebSocket>? _Socket;
        private WebSocket? _imabitch;
        private string name = "Joystick";


        public Joystick() {
            _imabitch = new WebSocket();
            _Socket = new WebsocketClient<Joystick.WebSocket>(_imabitch, "", "", default);
        }

        ~Joystick() {
            Dispose();
        }

        public void Dispose() {
            _imabitch?.Dispose(); // interface first, if nothing else GC won't touch the methods like Receive (BET?) until it's no longer needed.
            _Socket?.Dispose();

            GC.SuppressFinalize(this);
        }


        // Why the fuck am I offloading this? what do I gain from it? seriously. other than fucking headaches.
        public class WebSocket : IWebSocketService {
            //private string name = "Joystick:WebSocket";
            public string name { get; } = "WebSocket:Joystick";
            public string Host { get; } = "https://joystick.tv"; // for right now a temporary solution until config file is finished
            public string Endpoint { get; } = "wss://joystick.tv/cable"; // same
            public string Internal_Host { get; } = "joystick.tv"; // this is bad, if host changes anything then it'll look fucky, but one does not simply trim the start and expect good results. FUCK YOU SUBDOMAINS
            public Service Service { get; } = Service.Joystick;
            // The devil is god, and god is a liar.
            private string _channelId;
            private Func<string, Task<bool>>? _SendDelegate;
            private Func<Task<bool>>? _CloseDelegate;
            private Func<Task<bool>>? _ConnectDelegate;

            public string ChannelId => _channelId;

            ~WebSocket() {
                Dispose(); // Maybe I made a mistake making Emojiconical global.
            }

            public void Dispose() {
                _SendDelegate = null;
                _channelId = null!;


                GC.SuppressFinalize(this);
            }

            public Task<bool> Connect() {
                throw new NotImplementedException();
            }

            public Task Reconnect() {
                throw new NotImplementedException();
            }

            public Task<bool> Disconnect(bool faulted) {
                throw new NotImplementedException();
            }

            public Task Receive(string msg) {
                throw new NotImplementedException();
            }

            public void Whisper(params string[] moo) {
                throw new NotImplementedException();
            }

            public void Reply(params string[] moo) {
                throw new NotImplementedException();
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="action"></param>
            /// <param name="message"></param>
            /// <param name="whisperTarget"></param>
            /// <param name="messageId"></param>
            /// <exception cref="NotImplementedException"></exception>
            public async Task<bool> Send(params string[] moo) {
                if (_SendDelegate == null) throw new BotException(name, "Send method was not registered.");

                await Task.Delay(1); // Also fuck you again
                return true; //fuck you
                //return _SendDelegate != null ? await _SendDelegate.Invoke(moo) : false;
            }

            public void Kick(params string[] moo) {
                throw new NotImplementedException();
            }

            public void RegisterSend(Func<string, Task<bool>> SendDelegate) => _SendDelegate = SendDelegate;

            public void RegisterCloseAsync(Func<Task<bool>> CloseAsyncDelegate) => _CloseDelegate = CloseAsyncDelegate;

            public void RegisterConnectAsync(Func<Task<bool>> ConnectAsyncDelegate) => _ConnectDelegate = ConnectAsyncDelegate;

            /// <summary>
            ///  Constructs the string to send to the socket.
            /// </summary>
            /// <param name="action">The action. (Alternatively for subscription 'subscribe')</param>
            /// <param name="msg">Message</param>
            /// <param name="user">Username</param>
            /// <param name="msgid">Message Identifer</param>
            /// <returns><see cref="string"/> JSON Object</returns>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="BotException"></exception>"
            public string MessageConstructor(string action, string msg = "", string user = "", string msgid = "") { // Text, MessageID, Username are the only 3 parameers you'll ever need.
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
                                _channelId
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
                                _channelId
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
                        throw new BotException($"{this.name}:MessageConstructor", $"Invalid data type fall-thru. Data: {action}");
                }
            }

            private Task onMessage_StreamEvent(string payload) { // I have no idea what I was smoking when I wrote this.
                try {
                    RootStreamEvents? streamEvent = JsonSerializer.Deserialize<RootStreamEvents>(payload);

                    switch (streamEvent?.message.type ?? "noop") {
                        case "Started":
                            // stream started
                            Print("NT", "Your stream is now live.", PrintSeverity.Normal);
                            _ = Logger.LogAsync(name, new string[] { "Stream registered as live." });
                            return Task.CompletedTask;
                        case "StreamEnding": // Stream ending (pending state? maybe for reconnection attempt?)
                            /// noop - for now.
                            return Task.CompletedTask;
                        case "Ended": // Stream has ended
                            Print("NT", $"your stream has ended.", PrintSeverity.Normal);
                            _ = Logger.LogAsync(name, new string[] { "Stream registered as ended." });
                            return Task.CompletedTask;
                        case "ViewerCountUpdated": // Polled maybe? otherwise on actual change. it looks like it can actually generate 2 different ID's and fire them both
                            Console.Title = $"♥ Shimamura :: {streamEvent.message.Metadata.viewerCount.ToString()} ♥";
                            return Task.CompletedTask;
                        case "SettingsUpdated":
                            /// noop - for now, I might link this to the API call.
                            return Task.CompletedTask;
                        case "Tipped":
                            /// ===> This goes to Module eventually, for now create a class maybe or something to handle WebSocket connect to vNyan
                            /// This is also going to be the most tricky one to handle because you need to handle all client modules
                            /// assuming it is a 'Module' type tip.
                            var redeem = streamEvent?.message.text;
                            var redeemed = streamEvent?.message.Metadata.tipMenuItem;
                            var redeemer = streamEvent?.message.Metadata.who;
                            var cost = streamEvent?.message.Metadata.howMuch;
                            /*  I don't know if any of these fields can be nullable, if so then it could throw when it shouldn't.  */
                            /// I think they split(' ', 2) tip items before sending over socket, reasoning:
                            /// "Remove Bra for the Entire Stream" is a tip item, however I received "Remove Bra"
                            /// This was long ago though I don't think I log tips anymore / haven't got a tip in a long time.
                            /// For now to make it easy, I'm only going to go by the tip_cost
                            /// Investimagate.
                            /// 2025 - Yeah this is interest, the tip menu is still around and "Name the item" is "Remove Bra for the Entire Stream" idk.
                            /// streamEvent.message.text was the code at the time that logged which is what is displayed to chat? "{{Remove Bra}}"
                            /// hmm, yes no idea. I'll have to do a test on 1 tip and figure it out.


                            /// This seems like flawed logic but it's not because it's a server sent Tipped event.
                            /// This means that even because the fields are nullable, it will still only fire if a TIP event is sent.
                            switch (cost) {
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
                                        _ = SendMessage("send_message", $"{Heart_Purple} Thank you for the tip {streamEvent?.message.Metadata.who} ! {Heart_Purple}");
                                    break;
                            }
                            return Task.CompletedTask;
                        case "WheelSpinClaimed":
                            // Wheelspin tip - I do not believe you have implemnted any way of handling this yet, soo. DRAW THE FUCKING OWL
                            Print("", $"{streamEvent?.message.Metadata.who ?? "Unknown"} just spun the wheel and won {streamEvent.message.Metadata.prize} for {streamEvent.message.Metadata.howMuch} !", PrintSeverity.Normal);
                            // owl
                            break;
                        case "Followed": // You haz new fren
                            _ = SendMessage("send_message", $"Welcome to the {Cherry_Blossom} Cherry Blossoms {Cherry_Blossom} {streamEvent?.message.Metadata.who}. Thank you the Follow !");
                            Print("", $"A new follower has appeared! Say hi to {streamEvent?.message.Metadata.who}!", PrintSeverity.Normal);
                            return Task.CompletedTask;
                        case "FollowerCountUpdated":
                            // Noop - 
                            return Task.CompletedTask;
                        case "DeviceConnected": // You haz device connected and reported back by API
                            Print("", $"Your toy was registered as `{streamEvent?.message.text}` from Joystick", PrintSeverity.Normal);
                            // IDK probably not worth mentioning but I don't have a toy to test how connection works. If someone was actually running Shimararu it might be useful to know on the fly when it was registered.
                            return Task.CompletedTask;
                        default:
                            Print($"{this.name}:StreamEvent", $"Received a new Event that is not handled! EXCITING!", PrintSeverity.Debug);
                            _ = Logger.LogAsync($"{this.name}:WebSocket:StreamEvent:Discover L I M P", new string[] { $"Unhandled StreamEvent Raw :: ", payload });
                            break;
                    }
                } catch (Exception ex) { new BotException($"{this.name}:StreamEvent", $"Could not deserialize the WebSocket message.", ex); return Task.CompletedTask; }
                // Discover L I M P

                return Task.CompletedTask;
            }


            /// <summary>
            ///  Handles bang bot commands.
            /// </summary>
            /// <param name="message"><see cref="RootMessageEvent"/> deserialized message.</param>
            private Task OnBangCommand(RootMessageEvent msg) {
                using (VNyan vnyan = new()) {
                    var cmd = msg.message.text.Split('.')[1].ToLower();

                    switch (cmd) {
                        case "duck" or "yeet":
                            vnyan.Redeem(cmd);
                            break;
                        case "testing":
                            vnyan.Redeem("tta");
                            break;
                    }
                }

                return Task.CompletedTask;
            }


            private Task onMessage_Message(string payload) {
                RootMessageEvent? msg;

                try { msg = JsonSerializer.Deserialize<RootMessageEvent>(payload); } catch (Exception ex) { new BotException(name, $"Unable to deserialize OnMessage: {payload}", ex); return Task.CompletedTask; }
                ArgumentNullException.ThrowIfNullOrEmpty(msg.message.text, payload);
                ///chatHistory2.Add(msg.message.messageId);
                //if (chatHistory2[user_input])

                if (msg.message.text.ToLower().Contains("adachi91")) { if (_cooldown.IsRunning && _cooldown.ElapsedMilliseconds > 13_130) { _cooldown.Restart(); } else { _cooldown.Start(); if (WebUI!.Open && _cooldown.ElapsedMilliseconds < 13_000) _ = AudioOot.PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.HeyDumb, WebUI); } }
                if (msg.message.text.Contains("002") || msg!.message.text.Contains("zerotwo")) if (WebUI!.Open) WebUI.SendSSEImageAsync("https://steamuserimages-a.akamaihd.net/ugc/778494769436587920/675371BED432AF394DB2F145632671082F4779DF/?imw=5000\u0026imh=5000\u0026ima=fit\u0026impolicy=Letterbox\u0026imcolor=%23000000\u0026letterbox=false", 4); else new BotException(name, $"WebUI is not open.");
                if (msg.message.visibility != "public") { _ = Logger.LogAsync($"{this.name}:OnMessage", new string[] { $"Discover L I M P - NonPub msg: {payload}" }); return Task.CompletedTask; }// I think DM to bot only - not user. so this should be handled for bot-whisper interactions.
                if (msg.message.text.StartsWith('.')) { _ = OnBangCommand(msg); return Task.CompletedTask; }

                Print("Chat", $"{msg.message.author.username}: {msg.message.text}", PrintSeverity.Normal);


                _ = AudioOot.PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.Beep);
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
                    Print(this.name, $"Estasblished connection to chatroom.", PrintSeverity.Normal);
                    return Task.CompletedTask;
                } else if (data.Contains("reject_subscription")) {
                    //to log failures bypassing the buffer.
                    if (DEBUGGING_ENABLED /* DO NOT REMOVE THIS ONE. */ ) _ = Logger.LogAsync(this.name, new string[] { data });
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
        }


        /// <summary>
        ///  Handle the WebToken for Joystick.TV.
        /// </summary>
        public class WebToken {
            private string name = $"Joystick:WebToken";
#pragma warning disable CS8981
            private class validation
#pragma warning restore CS8981
            {
                [JsonPropertyName("exp")]
                public required int expiry { get; set; }
                [JsonPropertyName("nbf")]
                public required int not_before { get; set; }
                [JsonPropertyName("iat")]
                public required int issued_at { get; set; }
                [JsonPropertyName("aud")]
                public required string audience { get; set; }
                public required string bot_id { get; set; }
                public required string channel_id { get; set; }
            }

            private validation? _WebObject { get; set; }
            private string? _Token { get; set; } = null;

            ///==============================================================================\\\
            ///  A dumbstructor is what I call a non-constructor, acting like a constructor.   \\\
            /// ================================================================================ \\\

            /// <summary>
            ///  Checks if token hasn't expired (12Hr offset)
            /// </summary>
            /// <returns><see cref="bool"/> is expired</returns>
            public bool Expired => ((_WebObject?.expiry ?? 0) - GetUnixTimestamp() <= 43200);
            /// <summary>
            ///  Checks if a Global JWT Token exists
            /// </summary>
            /// <returns><see cref="bool"/> if valid token is held</returns>
            public bool Valid => !string.IsNullOrEmpty(ACCESS_TOKEN) && _WebObject != null;


            /// <summary>
            ///  _Dumbstructor: Parses token if is held, and not parsed already or is expired and needs to be parsed again.
            /// </summary>
            /// <returns>Bool - Success</returns>
            public async Task<bool> Token() {
                if (DEBUGGING_ENABLED) Print(name, $"Attempting to parse web token.", PrintSeverity.Debug);
                if (string.IsNullOrEmpty(ACCESS_TOKEN)) { Print(name, $"ACCESS_TOKEN IS EMPTY", PrintSeverity.Debug); return false; } // Short-Circuit - OAuth flow needs to happen, no token is held.


                try { await Parse(ACCESS_TOKEN); Print(name, $"Web Token succesfully stored.", PrintSeverity.Debug); return true; } catch { return false; }
            }

            public int? GetExpiration => _WebObject != null ? _WebObject.expiry : null;
            public int? GetNotBefore => _WebObject != null ? _WebObject.not_before : null;
            public int? GetIssuedTime => _WebObject != null ? _WebObject.issued_at : null;
            public string GetChannelIdentifier => _WebObject?.channel_id ?? null!;
            public string GetBotIdentifier => _WebObject?.bot_id ?? null!;

            /// <summary>
            ///  Parse a JSON Web Token and extract Payload.
            /// </summary>
            /// <param name="token">String - JWT</param>
            /// <exception cref="BotException"></exception>
            private Task Parse(string token) {
                string[] parts = token.Split('.');
                if (parts.Length != 3) throw new BotException(name, "Invalid Web Token Format.");

                string payload = parts[1]; // extract payload (header . payload . signatory)
                payload = payload.Replace('-', '+').Replace('_', '/');

                ///https://datatracker.ietf.org/doc/html/rfc7515#section-2 return any missing padding.
                switch (payload.Length % 4) {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                string convert = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

                try {
                    _WebObject = JsonSerializer.Deserialize<validation>(convert);
                } catch {
                    throw new BotException(name, "Could not validate the JWT Payload.");
                }
                return Task.CompletedTask;
            }
        }

        class Config {
            public record OAuth (string host, string client_id, string client_secret, string authorize_uri, string token_uri, string redirect_uri, string scope = "ALLURBASES", string response_type = "code", AuthTypes auth = AuthTypes.Basic, Service service = Service.Joystick);
            public record WebSocketConfig (string endpoint, string token, string channelId, Service service = Service.Joystick); // WiP
        }

        /// <summary>
        ///  OAuth2.0 Constructor for Joystick.tv.
        /// </summary>
        /// <remarks>Disposes the OAuthClient once it's finished so it doesn't linger in memory, including HTTPListener and all variables and OAuth codes.</remarks>
        public class OAuthConstructor : IDisposable {
            private bool _disposed = false;
            private OAuthClient? _OAuthClient;

            public OAuthConstructor(string host, string client_id, string client_secret, string authorize_uri, string token_uri, string redirect_uri, string response_type = "code", string scope = "ALLURBASES") {
                //_OAuthClient = new(host, client_id, );
            }

            //private 


            public void Dispose() {
                //Dispose(_disposed: true);
                _OAuthClient = null;
                GC.SuppressFinalize(this);
            }
        }

        /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// <remarks>This is only used in Using statements, do not dispose of this when cleaning up Service class.</remarks>
        internal class API : IDisposable // look, names are really hard for me. I can get stuck on a name instead of coding for a long time.
        {
            private readonly string name = "Stream-Settings";
            private HttpClient httpClient;
            private string apiUrl = $"{HOST}/api/users/stream-settings";

            public API() {
                httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(6) };
            }

            ~API() {
                Dispose();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="title">String - Title of the stream</param>
            /// <returns>Actually maybe something.</returns>
            public async Task SetTitleAsync(string title) => await UpdateFieldAsnyc(title);
            /// <summary>
            ///  Update the chatroom greeting message.
            /// </summary>
            /// <param name="msg">String - The message to set as greeting</param>
            /// <returns></returns>
            public async Task SetGreetingAsync(string msg) => await UpdateFieldAsnyc("", msg); //bitch. Again.. BITCH DONT TELL ME WHAT TO DO C#
            /// <summary>
            ///  Add word(s) to the banned words list for chat.
            /// </summary>
            /// <param name="word">String[] - Word(s)</param>
            /// <returns></returns>
            public async Task SetBannedWordAddAsync(string[] word) => await UpdateFieldAsnyc("", "", word.Prepend("add").ToArray());
            /// <summary>
            ///  Remove word(s) from the banned words list.
            /// </summary>
            /// <param name="word">String[] - Word(s)</param>
            /// <returns></returns>
            public async Task SetBannedWordRemoveAsync(string[] word) => await UpdateFieldAsnyc("", "", word.Prepend("remove").ToArray());


            /// <summary>
            ///  Update a field in the stream settings.
            /// </summary>
            /// <param name="title">String - Title of the stream</param>
            /// <param name="welcomeMsg">String - The welcome message displayed in chat</param>
            /// <param name="bannedWords">String[] - A collection of words to add to banned words list</param>
            /// <returns>Null - This is only here because this Task is not try-catch safe</returns>
            private async Task UpdateFieldAsnyc(string title = "", string welcomeMsg = "", string[]? bannedWords = null) { // so they think.
                if (!JWT.Valid || JWT.Expired) throw new BotException($"{this.name}:UpdateFieldAsync", $"No valid JWT to access API endpoint.");

                bool _update_title = false;
                bool _update_greeting = false;
                bool _update_banned_words = false;
                bool _add = false;
                string[] _merged_banned_words = [];

                if (bannedWords != null && bannedWords.Length > 1) {
                    _update_banned_words = true;
                    // literally impossible to happen unless your dumbass writes the wrong call, but hey it's a fail safe against yourself.
                    if (bannedWords[0] != "add" || bannedWords[0] != "remove") { new BotException($"{this.name}:UpdateFieldAsync", $"First index of bannedwords was not expected value. Value: {bannedWords[0]}"); return; }
                    _add = bannedWords[0] == "add" ? true : false;

                    bannedWords = bannedWords.Skip(1).ToArray(); // Pop shift whatever the add/remove out of the list. WINWQQQQQQQQQQQ ^^
                }

                if (!string.IsNullOrEmpty(welcomeMsg)) _update_greeting = true;
                if (!string.IsNullOrEmpty(title)) _update_title = true;

                StreamSettings? _currentSettings = await GetStreamSettings();

                if (_currentSettings == null) throw new BotException($"{this.name}:UpdateFieldAsync", $"Could not retrieve current settings.");

                if (_update_banned_words && _currentSettings.banned_chat_words != null)
                    _merged_banned_words = _add ? _currentSettings.banned_chat_words.Concat(bannedWords!).Distinct().ToArray() : _currentSettings.banned_chat_words.Where(item => !bannedWords!.Contains(item)).ToArray();

                var requestBody = new {
                    streamer = new {
                        stream_title = _update_title ? title : _currentSettings.stream_title,
                        chat_welcome_message = _update_greeting ? welcomeMsg : _currentSettings.chat_welcome_message,
                        banned_chat_words = _update_banned_words ? _merged_banned_words : (_currentSettings.banned_chat_words?.ToArray() ?? []),
                    }
                }.Stringify();

                await UpdateStreamSettingsAsync(requestBody);

                //draw the rest of the owl
            }

            /// <summary>
            ///  This is the HttpClient PATCH client.
            /// </summary>
            /// <param name="payload"><see cref="string"/> Settings to update.</param>
            /// <exception cref="BotException"></exception>
            private async Task UpdateStreamSettingsAsync(string payload) {
                var name = $"{this.name}:UpdateStreamSettingsAsync";

                if (!JWT.Valid || JWT.Expired)
                    throw new BotException(name, "No valid JWT to connect to Rest endpoint.");

                var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage(HttpMethod.Patch, apiUrl);
                requestMessage.Headers.Add("Authorization", $"Bearer {ACCESS_TOKEN}");
                requestMessage.Content = requestContent;

                try {
                    HttpResponseMessage? response = await httpClient.SendAsync(requestMessage);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                        var respBody = await response.Content.ReadAsStreamAsync();

                        if (string.IsNullOrEmpty(respBody.ToString())) throw new BotException(name, $"Response was empty");
                        StreamSettings resp = JsonSerializer.Deserialize<StreamSettings>(respBody)!; // This seems fucky watch it.
                    } else {
                        throw new BotException(name, $"Http error occured (Http Status: {(int)response.StatusCode})");
                    }
                }
                catch (BotException) { /* prevent recursive */ }
                catch (HttpRequestException Hex) { new BotException(name, "Unhandled HTTPRequestException.", Hex); /* double catch? --idk what this means */ }
                catch (Exception ex) { new BotException(name, $"Unhandled exception.", ex); }
            }

            /// <summary>
            ///  Returns the current stream settings from the Joystick.tv RestAPI using the current JWT.
            /// </summary>
            /// <returns>T-StreamSettings || NULL - Instance of current settings.</returns>
            public async Task<StreamSettings?> GetStreamSettingsAsync(bool si = false) => await GetStreamSettings(null, si);

            /// <summary>
            ///  Gets the current Stream Settings from the API Endpoint.
            /// </summary>
            /// <returns>StreamSettings || null</returns>
            private async Task<StreamSettings?> GetStreamSettings(string? token = null, bool silent = false) {
                if (!JWT.Valid || JWT.Expired) { if (silent) return null; new BotException($"{this.name}:GetStreamSettings", "No valid token to connect to API endpoint."); return null; }
                var req = new StringContent("", Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("Authorization", $"Bearer {token ?? ACCESS_TOKEN}");
                request.Content = req;
                try {
                    var response = await httpClient.SendAsync(request, CancellationToken.None).ConfigureAwait(false);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                        var respBody = await response.Content.ReadAsStringAsync();
                        StreamSettings? deserialized_response = JsonSerializer.Deserialize<StreamSettings>(respBody);

                        //Print(this.name, $"Passed http 200 :: {deserialized_response?.chat_welcome_message ?? "No greeting is set!"}", PrintSeverity.Debug);
                        return deserialized_response;
                    }
                    new BotException($"{this.name}:GetStreamSettings", $"Failed to retrieve Stream Settings. (Http: {response.StatusCode} :: {response.Content.ToString()})");
                } catch (HttpRequestException Hex) {
                    new BotException($"{this.name}:GetStreamSettings", $"An error occured while trying to retrieve stream settings.", Hex); // I need to come back around and fix all these.
                } catch (Exception ex) {
                    new BotException($"{this.name}:GetStreamSettings", $"Unhandled Exception.", ex);
                }
                return null;
            }


            public async Task RunTests(string oldToken, string currentToken_Diff) {
                Print(name, "Starting test", PrintSeverity.Debug);
                var a = await GetStreamSettings(oldToken);
                Print(name, $"Test 1 complete :: stream_title: '{a?.stream_title}'", PrintSeverity.Debug);

                Print(name, "Starting test 2", PrintSeverity.Debug);
                // Sprinkle cocaine in here before running.

                var b = await GetStreamSettings(currentToken_Diff);
                Print(name, $"Test 2 complete :: stream_title: '{b?.stream_title}'", PrintSeverity.Debug);

                Print(name, "All tests done.", PrintSeverity.Debug);
            }




            public void Dispose() {
                httpClient?.Dispose();
                apiUrl = string.Empty;
                GC.SuppressFinalize(this);
            }

            #region StreamSettings_RestAPI_JSON
            public class StreamSettings {
                /// <summary>
                ///  API getter;
                /// </summary>
                public required string username { get; set; }
                /// <summary>
                ///  API getter; setter;
                /// </summary>
                /// <remarks>This field is allowed to be empty.</remarks>
                public string? stream_title { get; set; }
                /// <summary>
                ///  API getter; setter;
                /// </summary>
                /// <remarks>This field is allowed to be empty.</remarks>
                public string? chat_welcome_message { get; set; }
                /// <summary>
                ///  API getter; setter;
                /// </summary>
                public List<string>? banned_chat_words { get; set; }
                /// <summary>
                ///  API getter;
                /// </summary>
                public required bool device_active { get; set; }
                /// <summary>
                ///  API getter;
                /// </summary>
                public required string photo_url { get; set; }
                /// <summary>
                ///  API getter;
                /// </summary>
                public required bool live { get; set; }
                /// <summary>
                ///  API getter;
                /// </summary>
                public required int number_of_followers { get; set; }
            }
            #endregion
        }
    }
}
