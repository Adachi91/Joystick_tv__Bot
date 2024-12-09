using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes
{
    class Joystick {
        public Joystick() { }



        public class WebSocket {

        }

        internal class API : IDisposable // look, names are really hard for me. I can get stuck on a name instead of coding for a long time.
        {
            private HttpClient httpClient;
            private string apiUrl = $"{HOST}/api/users/stream-settings";
            private string name = "Stream-Settings";

            public API() {
                httpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(6) };
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="title">String - Title of the stream</param>
            /// <returns>Actually maybe something.</returns>
            public Task SetTitleAsync(string title) => _ = UpdateFieldAsnyc(title);
            /// <summary>
            ///  Update the chatroom greeting message.
            /// </summary>
            /// <param name="msg">String - The message to set as greeting</param>
            /// <returns></returns>
            public Task SetGreetingAsync(string msg) => _ = UpdateFieldAsnyc("", msg); //bitch. Again.. BITCH DONT TELL ME WHAT TO DO C#
            /// <summary>
            ///  Add word(s) to the banned words list for chat.
            /// </summary>
            /// <param name="word">String[] - Word(s)</param>
            /// <returns></returns>
            public Task SetBannedWordAdd(string word) => _ = UpdateFieldAsnyc("", "", new string[] { "add", word });
            /// <summary>
            ///  Remove word(s) from the banned words list.
            /// </summary>
            /// <param name="word">String[] - Word(s)</param>
            /// <returns></returns>
            public Task SetBannedWordRemove(string word) => _ = UpdateFieldAsnyc("", "", new string[] { "remove", word });


            /// <summary>
            ///  Update a field in the stream settings.
            /// </summary>
            /// <param name="title">String - Title of the stream</param>
            /// <param name="welcomeMsg">String - The welcome message displayed in chat</param>
            /// <param name="bannedWords">String[] - A collection of words to add to banned words list</param>
            /// <returns>Null - This is only here because this Task is not try-catch safe</returns>
            private async Task UpdateFieldAsnyc(string title = "", string welcomeMsg = "", string[]? bannedWords = null) { // so they think.
                if (!JWT.Valid || JWT.Expired) throw new BotException($"{this.name}:UpdateFieldAsync", $"No valid JWT to access API endpoint.");
                if (DEBUGGING_ENABLED) Print($"{this.name}:UpdateFieldAsync", $"UpdateFieldAsnyc Requested: Title: {title} :: Greeting: {welcomeMsg} :: BannedWords: {bannedWords ?? null}", PrintSeverity.Debug);

                bool _update_title = false;
                bool _update_greeting = false;
                bool _update_banned_words = false;
                bool _add = false;
                string[] _merged_banned_words = null;

                if (bannedWords != null && bannedWords.Length > 1) {
                    _update_banned_words = true;
                    // literally impossible to happen unless your dumbass writes the wrong call, but hey it's a fail safe against yourself.
                    if (bannedWords[0] != "add" || bannedWords[0] != "remove") { new BotException($"{this.name}:UpdateFieldAsync", $"First index of bannedwords was not expected value. Value: {bannedWords[0]}"); return; }
                    _add = bannedWords[0] == "add" ? true : false;

                    bannedWords = bannedWords.Skip(1).ToArray(); // Pop shift whatever the add/remove out of the list. WINWQQQQQQQQQQQ ^^
                }

                if (!string.IsNullOrEmpty(welcomeMsg)) _update_greeting = true;
                if (!string.IsNullOrEmpty(title)) _update_title = true;

                // Get StreamSettings - Good you were smart enough to remember you need to get settings first. Dork.
                if (DEBUGGING_ENABLED) Print($"{this.name}", $"Attempting to retrieve current settings.", PrintSeverity.Debug);

                StreamSettings? _currentSettings = await GetStreamSettings(); /// I CANNOT CURRENTLY PASS THIS POINT. To my recollection the httpclient cancellation token expires instantly.
                if (_currentSettings == null) throw new BotException($"{this.name}:UpdateFieldAsync", $"Could not retrieve current settings.");

                // Send UpdateStreamSettingsAsync() with mixed params of old settings and updated settings.
                if (_update_banned_words)
                    _merged_banned_words = _add ? _currentSettings.banned_chat_words.Concat(bannedWords).Distinct().ToArray() : _currentSettings.banned_chat_words.Where(item => !bannedWords.Contains(item)).ToArray();

                var requestBody = new {
                    streamer = new {
                        stream_title = _update_title ? title : _currentSettings.stream_title,
                        chat_welcome_message = _update_greeting ? welcomeMsg : _currentSettings.chat_welcome_message,
                        banned_chat_words = _update_banned_words ? _merged_banned_words : _currentSettings.banned_chat_words.ToArray(),
                    }.Stringify()
                }.Stringify();

                //Print(this.name, $"Sendraw: '{request}'", PrintSeverity.Debug);

                if (DEBUGGING_ENABLED) Print(this.name, $"Calling UpdateStreamSettingAsync({requestBody})", PrintSeverity.Debug);
                _ = UpdateStreamSettingsAsync(requestBody);

                //draw the rest of the owl
            }

            // Deprecated - Maybe not?
            private async Task UpdateStreamSettingsAsync(string payload) {
                var name = $"{this.name}:UpdateStreamSettingsAsync";

                if (DEBUGGING_ENABLED) Print(name, $"XXX UpdateStreamSettingAsync received.", PrintSeverity.Debug);
                if (!JWT.Valid || JWT.Expired)
                    throw new BotException(name, "No valid JWT to connect to Rest endpoint.");

                if (DEBUGGING_ENABLED) Print(name, $"Constructing HTTP payload.", PrintSeverity.Debug);
                var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage(HttpMethod.Patch, apiUrl);
                requestMessage.Headers.Add("Authorization", $"Bearer {ACCESS_TOKEN}");
                requestMessage.Content = requestContent;
                if (DEBUGGING_ENABLED) Print(name, $"XXX Request fully constructed.\r\nAttempting to send.", PrintSeverity.Debug);

                try {
                    HttpResponseMessage? response = await httpClient.SendAsync(requestMessage);

                    if (DEBUGGING_ENABLED) Print(name, $"XXX Request sent.", PrintSeverity.Debug);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                        var respBody = await response.Content.ReadAsStreamAsync();

                        if (string.IsNullOrEmpty(respBody.ToString())) throw new BotException(name, $"Response was empty");
                        if (DEBUGGING_ENABLED) Print(name, $"XXX Yeah bitch! SCIENCE", PrintSeverity.Debug);
                        if (DEBUGGING_ENABLED) Print(name, $"Response: {response.Content.ToString()}", PrintSeverity.Debug);
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
            public async Task<StreamSettings?> GetStreamSettingsAsync() => await GetStreamSettings();

            /// <summary>
            ///  Gets the current Stream Settings from the API Endpoint.
            /// </summary>
            /// <returns>StreamSettings || null</returns>
            private async Task<StreamSettings?> GetStreamSettings(string? token = null) {
                if (!JWT.Valid || JWT.Expired) { new BotException($"{this.name}:GetStreamSettings", "No valid token to connect to API endpoint."); return null; }
                Print(this.name, "Passed check 1", PrintSeverity.Debug);
                var req = new StringContent("", Encoding.UTF8, "application/json");
                using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);

                request.Headers.Add("Authorization", $"Bearer {token ?? ACCESS_TOKEN}");
                request.Content = req;
                Print(this.name, "Passed check 2", PrintSeverity.Debug);
                try {
                    var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                    Print(this.name, "Passed check http req", PrintSeverity.Debug);
                    if (response.StatusCode == System.Net.HttpStatusCode.OK) {
                        var respBody = await response.Content.ReadAsStringAsync();
                        StreamSettings? deserialized_response = JsonSerializer.Deserialize<StreamSettings>(respBody);

                        Print(this.name, $"Passed http 200 :: {deserialized_response?.chat_welcome_message ?? "No greeting is set!"}", PrintSeverity.Debug);
                        return deserialized_response;
                    }
                    //Print("REST", $"ERroR: {response.StatusCode} :: {response.Content.ToString()}", PrintSeverity.Debug);
                    new BotException($"{this.name}:GetStreamSettings", $"Failed to retrieve Stream Settings. (Http: {response.StatusCode} :: {response.Content.ToString()})");
                    return null;
                } catch (HttpRequestException ex) {
                    Console.WriteLine("Is this unobservable?");
                    new BotException($"{this.name}:GetStreamSettings", $"An error occured while trying to retrieve stream settings.", ex); // I need to come back around and fix all these.
                    return null;
                } catch (Exception Sex) {
                    /// DEBUG AREA ============================================================================================================================
                    // Unobservable uncaught exception being thrown by CancellationToken
                    Console.WriteLine($"The fuck? {Sex}");
                    return null;
                }
            }


            public async Task RunTests(string oldToken, string currentToken_Diff) {
                Print(name, "Starting test", PrintSeverity.Debug);
                var a = await GetStreamSettings(oldToken);
                Print(name, $"Test 1 complete :: stream_title: '{a.stream_title}'", PrintSeverity.Debug);

                Print(name, "Starting test 2", PrintSeverity.Debug);
                // Sprinkle cocaine in here before running.

                var b = await GetStreamSettings(currentToken_Diff);
                Print(name, $"Test 2 complete :: stream_title: '{b.stream_title}'", PrintSeverity.Debug);

                Print(name, "All tests done.", PrintSeverity.Debug);
            }




            public void Dispose() {
                httpClient?.Dispose();
                apiUrl = string.Empty;
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
