using ShimamuraBot.Classes;
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
        private string name = "Discord";
        private string? _ctx_msg = "I'm live now! check out my stream.";
        private string? _ctx_descriptor;
        private string _webHookUri;

        /// <summary>
        ///  Construct the DiscordWebHook client.
        /// </summary>
        /// <param name="webHookUri">Your Discord Webhook URI</param>
        /// <param name="msg">Optional - Custom message (Otherwise Streams Title)</param>
        /// <param name="description">Optional - Custom description of your stream</param>
        public DiscordWebhook(string webHookUri, string msg = "", string description = "") {
            //_ctx_msg = msg;
            //_ctx_descriptor = description;
            _webHookUri = webHookUri;
        }

        public async Task SendDiscordWebHookAsync() => await SendHookAsync();

        private async Task SendHookAsync() {
            try {
                using (var client = new HttpClient()) { //reuse grab info from Joystick.tv construct discord embed, and reuse httpClient and send webhook.
                    Joystick.API.StreamSettings? streamSettings;

                    using (Joystick.API JoystickAPI = new()) {
                        streamSettings = await JoystickAPI.GetStreamSettingsAsync();
                        if (string.IsNullOrEmpty(streamSettings?.username)) throw new BotException(name, "Could not retrieve stream settings.");

                        var payload = new {
                            //content = _ctx_msg,
                            embeds = new[] {
                                new {
                                    title = "xPlaceholderx I'm live.",
                                    url = $"https://www.joystick.tv/u/{streamSettings?.username ?? "joystickdottv"}",
                                    description = streamSettings?.stream_title ?? "Unavailable",
                                    image = new { url = streamSettings?.photo_url ?? @"https://avatars.githubusercontent.com/u/127134057?s=200&v=4" },
                                    footer = new {
                                        text = "Joystick.tv"
                                    },
                                    timestamp = DateTime.UtcNow,
                                }
                            }
                        }.Stringify();

                        Print(name, $"JSON: '{payload}'", PrintSeverity.Debug);
                        return;

                        var content = new StringContent(payload, Encoding.UTF8, "application/json");

                        var discresp = await client.PostAsync(_webHookUri, content);

                        if (!discresp.IsSuccessStatusCode) {
                            throw new BotException(name, $"There was an error trying to post webhook to discord :: http status: {discresp.StatusCode}");
                        }
                    }
                }
            } catch (BotException) { } catch (Exception ex) {
                new BotException(name, "Unhandled Exception", ex);
            }
        }
    }
}
