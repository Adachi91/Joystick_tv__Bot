using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShimamuraBot.Modules
{
    internal class DiscordWebhook
    {
        private string _ctx_msg = "I'm live now! check out my stream.";
        private string _ctx_descriptor = string.Empty;
        private string _webHookUri = string.Empty;

        /// <summary>
        ///  Construct the DiscordWebHook client.
        /// </summary>
        /// <param name="webHookUri">Your Discord Webhook URI</param>
        /// <param name="msg">Optional - Custom message (Otherwise Streams Title)</param>
        /// <param name="description">Optional - Custom description of your stream</param>
        public DiscordWebhook(string webHookUri, string msg = "", string description = "") {
            _ctx_msg = msg;
            _ctx_descriptor = description;
            _webHookUri = webHookUri;
        }

        public async Task SendDiscordWebHook() =>
            await SendHook();

        private async Task SendHook() {
            StreamSettings streamSettings;

            try {
                using (var client = new HttpClient()) { //reuse grab info from Joystick.tv construct discord embed, and reuse httpClient and send webhook.
                    #region Joystick.TV.GetStreamSettings
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ACCESS_TOKEN);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var response = await client.GetAsync("https://joystick.tv/api/users/stream-settings");

                    if (response.IsSuccessStatusCode) {
                        var resp = await response.Content.ReadAsStringAsync();
                        streamSettings = JsonSerializer.Deserialize<StreamSettings>(resp);
                    }
                    else {
                        throw new BotException("Discord Webhook", $"Could not retrieve the information from Joystick.tv. Please make sure you have a valid token. Type `exp` :: http status: {response.StatusCode}");
                    }
                    #endregion

                    var payload = new {
                        content = _ctx_msg,
                        embeds = new[] {
                        new {
                            title = streamSettings.StreamTitle,
                            url = "https://www.joystick.tv/u/" + streamSettings.Username,
                            description = "Description of the stream",
                            image = new { url = streamSettings.PhotoUrl }
                        }
                    }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var discresp = await client.PostAsync(_webHookUri, content);

                    if (!discresp.IsSuccessStatusCode) {
                        throw new BotException("Discord Webhook", $"There was an error trying to post webhook to discord :: http status: {discresp.StatusCode}");
                    }
                }
            }
            catch (BotException) {} catch (Exception ex) {
                new BotException("Discord Webhook", "General Error", ex);
            }
        }

        #region streamSettingclass
        public class StreamSettings {
            public string Username { get; set; }
            public string StreamTitle { get; set; }
            public string PhotoUrl { get; set; }
            public bool DeviceActive { get; set; }
        }
        #endregion
    }
}
