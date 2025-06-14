using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.Json.Serialization;

namespace ShimamuraBot
{
    // Small steps you can do it
    // hang in there kitten poster here
    public class Config
    {
        private static string name = "Settings-Manager";
        public static Dictionary<string, string> vtuberModule = new Dictionary<string, string>();
        private static SemaphoreSlim _writer = new SemaphoreSlim(1,1);

        /// <summary>
        ///  Loads the environment file
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void load() {
            string _logging = string.Empty;
            string _vnyan_hook = string.Empty;
            string _configPth = string.Empty;
            try {
                foreach (var line in File.ReadAllLines(ENVIRONMENT_PATH!)) {
                    if (line.StartsWith("#") || line.StartsWith("//") || line.StartsWith("--") || string.IsNullOrEmpty(line)) continue;
                    var split = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length != 2) continue;

                    /* Absolutely hilarious. GPT suggested all the paths I wrote down. 
                     * 1) Reliquish external environmental variables to the buffer. (JSON) Something like Secrets.JSON
                     * 2)  (^) Sqlite it. (This could be configured by WebInterface. WebInterface.OnStart => Launch WebUI)
                     * 3) Overload the shit out of .env
                     */

                    var envKey = split[0] switch {
                        "HOST" => HOST = split[1],
                        "CLIENT_ID" => CLIENT_ID = split[1] ?? throw new HttpProtocolException(404, "WHAT THE FUCK", new Exception("FUCK YOU")),
                        "CLIENT_SECRET" => CLIENT_SECRET = split[1],
                        "WSS_HOST" => WSS_HOST = split[1],
                        "ACCESS_TOKEN" => ACCESS_TOKEN = split[1],
                        "REFRESH_TOKEN" => REFRESH_TOKEN = split[1],
                        "TWITCH_CLIENT_ID" => TWITCH_CLIENT_ID = split[1],
                        "TWITCH_CLIENT_SECRET" => TWITCH_CLIENT_SECRET = split[1],

                        "LOGGING" => _logging = split[1],
                        "DISCORDHOOK" => DISCORD_URI = split[1],
                        "CONFIG" => _configPth = split[1],
                        "VNYAN" => _vnyan_hook = split[1],
                        "USERNAME" => USERNAME = split[1],
                        /// Why did you disable this, it wills till be needed.
                        /// v
                        // "DEBUGGING_ENABLED" => DEBUGGING_ENABLED = Convert.ToBoolean(split[1]),
                        _ => null//throw new BotException(name, $"{split[0]} The Enviroment Keys in are not structured properly in the .env file{Environment.NewLine}The minimum is required{Environment.NewLine}HOST=HOST_URL{Environment.NewLine}CLIENT_ID=YOUR_CLIENT_ID{Environment.NewLine}CLIENT_SECRET=YOUR_CLIENT_SECRET{Environment.NewLine}WSS_HOST=THE_WSS_ENDPOINT{Environment.NewLine}")
                    };
                }
            } catch (Exception ex) {
                throw new BotException(name, $"Unable to read .env file.", ex);
            }

            if (string.IsNullOrEmpty(HOST) || string.IsNullOrEmpty(CLIENT_ID) || string.IsNullOrEmpty(CLIENT_SECRET) || string.IsNullOrEmpty(WSS_HOST))
                throw new BotException(name, $"One or more values in the environment file was not found{Environment.NewLine}The minimum is required{Environment.NewLine}HOST=HOST_URL{Environment.NewLine}CLIENT_ID=YOUR_CLIENT_ID{Environment.NewLine}CLIENT_SECRET=YOUR_CLIENT_SECRET{Environment.NewLine}WSS_HOST=THE_WSS_ENDPOINT{Environment.NewLine}");

            if (!string.IsNullOrEmpty(_logging)) try { LOGGING_ENABLED = Convert.ToBoolean(_logging); } catch { LOGGING_ENABLED = false; new BotException(name, "LOGGING Variable is not a valid value. Defaulting to False. (Valid opt: True, False)"); }
            if (!string.IsNullOrEmpty(_vnyan_hook)) try { if (Convert.ToBoolean(_vnyan_hook) == true) vNyan = new VNyan(); } catch (Exception ex) { new BotException(name, "Unable to parse boolean of vNyan environment setting.", ex); }

            /// This is a TODO - seperate vital config & personal settings.
            //unloading all the config from environment file and storing it in a seperate config.json
            if (!string.IsNullOrEmpty(_configPth)) { if (File.Exists(_configPth)) load_config(_configPth); else new BotException("Enviroment-Config-Loader", $"Could not find the directory {_configPth}. Please make sure the file exists here."); }
        }

        public class Ports {
            [JsonPropertyName("OAuth")]
            public int? OAuthLoopBack { get; set; }
            public int? vNyan { get; set; }
            public int? WebInterface { get; set; }
        }

        // Now I need to make a config.json file, and load it. I think I will make a class for it. But I need to make sure it's not a static class. HOWEVER, most importantly,
        // I need to make sure it's not a singleton. I think I will make a class for it. But we still need to smuggle in the 4000? kilos of cocaine. I think I will make a class for it.
        public class ShimamuraConfig {
            [JsonPropertyName("enable_logging")]
            public bool? logging { get; set; }
            public bool? vNyan { get; set; }
            public required Ports Ports { get; set; }
            public string? Username { get; set; }
            [JsonPropertyName("Debug")]
            public bool? Verbose { get; set; }
        }

        /*
         * {
         *      "Services": {
         *          "Joystick": {
         *              "Host": string,
         *              "Client_Id": string,
         *              "Client_Secret": string,
         *              "WebSocket_Host": string,
         *              "Access_Token": string,
         *              "Refresh_Token": string,
         *          },
         *          "Twitch": {
         *              "Host": string,
         *              "Client_Id": string,
         *              "Client_Secret": string,
         *              "Access_Token": string,
         *              "Refresh_Token": string,
         *          }
         *      },
         *      "Settings": {
         *          "Logging": bool,
         *          "vNyan": bool,
         *          "Debug": bool,
         *          "Botname": string,
         *          "Username": string,
         *          "DiscordWebhook": {
         *              "Url": string,
         *              "Enabled": bool,
         *          }
         *          "Yeet _thisprogram": true,
         *          "FluffyPoints": {
         *              "PointsName": string,
         *              "Enabled": bool,
         *          },
         *          "ExternalPing": string, // for controling what connection is made outside the users machine.
         *          
         *          "Ports": {
         *              "OAuth": int,
         *              "vNyan": int,
         *              "WebInterface": int, // I know API, WebInterface, and SSE's are all servied on the same port so WebInterface is probably best
         *          }
         *      }
         * }
         * 
         */

        //eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeh,
        //you're not Drake Chat-eh Co-pilot.
        //I'm not going to do that.
        // Don't you talk back to me.
        // I'm not going to do that.
        // LISTEN HERE YOU LITTLE SHIT


        public ServiceConfig? Joystick { get; set; }
        public ServiceConfig? Twitch { get; set; }
        public ServiceConfig? Discord { get; set; }
        public ShimamuraConfig? GeneralSettings { get; set; }

        public class ServiceConfig {
            /// <remarks>This is also used for discord hooks, it will be the hook uri, probably bad but whatever</remarks>
            public string? Host { get; set; }
            public string? Client_Id { get; set; }
            public string? Client_Secret { get; set; }
            /// <remarks>This needs to be WSS otherwise it will reject connection. Only vNyan is Non-TLS</remarks>
            public string? WebSocket_Host { get; set; }
            /// <remarks>Access Token or JSON Web Token</remarks>
            public string? Access_Token { get; set; }
            public string? Refresh_Token { get; set; }
        }


        private static void load_config(string fp) { // This should be named load_settings as it loads settings vs 'config' for endpoints/credentials.
            // TODO make a config struct
            // deserialize config
        }


        /// <summary>
        ///  Writes Token information to the environment file
        /// </summary>
        /// <param name="_defaults">(Optional)Boolean - Reset the .env file</param>
        public static async Task FlushToDisk(bool _defaults = false) { // Create a test to delete ACCESS_TOKEN, then call THIS
            if (!File.Exists(ENVIRONMENT_PATH)) { File.Create(ENVIRONMENT_PATH ?? ".env"); await FlushToDisk(true); }

            Dictionary<string, string> env;
            await _writer.WaitAsync();

            try {
                if (_defaults) {
                    env = new Dictionary<string, string> {
                        ["HOST"] = "https://example.net",
                        ["CLIENT_ID"] = "YOUR_CLIENT_ID",
                        ["CLIENT_SECRET"] = "YOUR_CLIENT_SECRET",
                        ["WSS_HOST"] = "WSS_ENDPOINT",
                        ["LOGGING"] = "False"
                    };
                } else {
                    string[] lines = File.ReadAllLines(ENVIRONMENT_PATH ?? throw new BotException(name, "No path."));
                    env = lines.Select(line => line.Split('=')).Where(parts => parts.Length == 2).ToDictionary(parts => parts[0], parts => parts[1]);

                    env["ACCESS_TOKEN"] = ACCESS_TOKEN ?? "";
                    env["REFRESH_TOKEN"] = REFRESH_TOKEN ?? ""; // DO NOT REMOVE OR I WILL BREAK YOUR LEGS
                    env["LOGGING"] = LOGGING_ENABLED.ToString() ?? "False";
                    env["USERNAME"] = USERNAME ?? "";
                    env["DISCORDHOOK"] = DISCORD_URI ?? "";
                    env["DEBUGGING_ENABLED"] = DEBUGGING_ENABLED.ToString() ?? "False";
                }

                var values = env.Select(kv => $"{kv.Key}={kv.Value}");

                File.WriteAllLines(ENVIRONMENT_PATH ?? throw new BotException(name, "No path."), values);
            } catch (Exception ex) {
                new BotException(name, "Unhandled exception.", ex);
            } finally {
                _writer.Release();
            }
        }


        public class Modules
        {
            private string path { get; set; }
            private string name { get; set; }
            private string command { get; set; }
            private string application { get; set; }
            private int value { get; set; }

            // vNyan should be loaded here as well. - I don't think VseeFace has any API/Websocket support.

            private object VTS_Hotkey = new { // I need to download VTube Studio and figure out it's API to finish this part.
                apiName = "VTubeStudioPublicAPI",
                apiVersion = "1.0",
                requestID = "ABC", //OAuthClient.getNounce(), // Figure it out you are not allowed to use this I will beat you
                messageType = "HotkeysInCurrentModelRequest",
                data = new {
                    modelID = "optional",
                    live2DItemFileName = "optional"
                }
            };

            ///private static

                /*
                 * 
                 *  { //sound
                 *      "application": "native",
                 *      "path": "assets/sounds/a.wav",
                 *      "name": "Alert1",
                 *  }
                 *  
                 *  {
                 *      "application": "vnyan",
                 *      "command": "yeet",
                 *     
                 *  }
                 *  
                 *  {
                 *      "module": "native",
                 *      "data": [
                 *          "type": "sound",
                 *          "
                 *      ]
                 *  }
                 *  
                 */


            public class VTS_AvailableHotkey
            {
                public string name { get; set; }
                public string type { get; set; }
                public string description { get; set; }
                public string file { get; set; }
                public string hotkeyID { get; set; }
                public List<object> keyCombination { get; set; }
                public int? onScreenButtonID { get; set; }
            }

            public class VTS_Data
            {
                public bool? modelLoaded { get; set; }
                public string modelName { get; set; }
                public string modelID { get; set; }
                public List<VTS_AvailableHotkey> availableHotkeys { get; set; }
            }

            public class VTS_Root
            {
                public string apiName { get; set; }
                public string apiVersion { get; set; }
                public long? timestamp { get; set; }
                public string requestID { get; set; }
                public string messageType { get; set; }
                public VTS_Data data { get; set; }
            }

        }

        /// <summary>
        ///  Load specific module settings such as vNyan, VTuber Studio, etc
        /// </summary>
        /// <param name="path">File Destination</param>
        public static void load_Modules(string path) { ///I think I want to make this a different project and load it as a dll
            if(File.Exists(path)) {
                
            } else {
                Print("ModuleLoader", $"Could not find the file specified", PrintSeverity.Error);
            }
        }
    }
}
