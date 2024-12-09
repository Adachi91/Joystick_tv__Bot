using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ShimamuraBot
{
    public static class envManager
    {
        private static string name = "Environment-Manager";
        public static Dictionary<string, string> audioModule = new Dictionary<string, string>();
        public static Dictionary<string, string> vtuberModule = new Dictionary<string, string>();
        private static SemaphoreSlim _writer = new SemaphoreSlim(1,1);

        /// <summary>
        ///  Loads the environment file
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void load() {
            //string tmp = null; //hold the long in a string for this type of switch to work.
            string _logging = null;
            string _vnyan_hook = null;
            string _configPth = null;
            try
            {
                foreach (var line in File.ReadAllLines(ENVIRONMENT_PATH))
                {
                    if (line.StartsWith("#") || line.StartsWith("//") || line.StartsWith("--") || string.IsNullOrEmpty(line)) continue;
                    var split = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

                    if (split.Length != 2) continue;

                    var envKey = split[0] switch
                    {
                        "HOST" => HOST = split[1],
                        "CLIENT_ID" => CLIENT_ID = split[1],
                        "CLIENT_SECRET" => CLIENT_SECRET = split[1],
                        "WSS_HOST" => WSS_HOST = split[1],
                        "ACCESS_TOKEN" => ACCESS_TOKEN = split[1],
                        "REFRESH_TOKEN" => REFRESH_TOKEN = split[1],

                        "LOGGING" => _logging = split[1],
                        "DISCORDHOOK" => DISCORD_URI = split[1],
                        "CONFIG" => _configPth = split[1],
                        "VNYAN" => _vnyan_hook = split[1],
                        _ => null//throw new BotException(name, $"{split[0]} The Enviroment Keys in are not structured properly in the .env file{Environment.NewLine}The minimum is required{Environment.NewLine}HOST=HOST_URL{Environment.NewLine}CLIENT_ID=YOUR_CLIENT_ID{Environment.NewLine}CLIENT_SECRET=YOUR_CLIENT_SECRET{Environment.NewLine}WSS_HOST=THE_WSS_ENDPOINT{Environment.NewLine}")
                    };
                }
            } catch (Exception ex) {
                throw new BotException(name, $"Unable to read .env file.", ex);
            }

            if (string.IsNullOrEmpty(HOST) || string.IsNullOrEmpty(CLIENT_ID) || string.IsNullOrEmpty(CLIENT_SECRET) || string.IsNullOrEmpty(WSS_HOST))
                throw new BotException(name, $"One or more values in the environment file was not found{Environment.NewLine}The minimum is required{Environment.NewLine}HOST=HOST_URL{Environment.NewLine}CLIENT_ID=YOUR_CLIENT_ID{Environment.NewLine}CLIENT_SECRET=YOUR_CLIENT_SECRET{Environment.NewLine}WSS_HOST=THE_WSS_ENDPOINT{Environment.NewLine}");

            if (!string.IsNullOrEmpty(_logging)) try { LOGGING_ENABLED = Convert.ToBoolean(_logging); } catch { LOGGING_ENABLED = false; new BotException(name, "LOGGING Variable is not a valid value. Defaulting to False. (Valid opt: True, False)"); }
            if (!string.IsNullOrEmpty(_vnyan_hook)) try { if(Convert.ToBoolean(_vnyan_hook) == true) vNyan = new VNyan(); } catch { new BotException(name, "Unable to parse boolean of vNyan environment setting."); }

            /// This is a TODO - seperate vital config & personal settings.
            //unloading all the config from environment file and storing it in a seperate config.json
            if (!string.IsNullOrEmpty(_configPth)) { if (File.Exists(_configPth)) load_config(_configPth); else new BotException("Enviroment-Config-Loader", $"Could not find the directory {_configPth}. Please make sure the file exists here."); }
        }

        private static void load_config(string fp) {
            // TODO make a config struct
            // deserialize config
        }


        /// <summary>
        ///  Writes Token information to the environment file
        /// </summary>
        /// <param name="_defaults">(Optional)Boolean - Reset the .env file</param>
        public static async void FlushToDisk(bool _defaults = false) { // Create a test to delete ACCESS_TOKEN, then call THIS
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
                    string[] lines = File.ReadAllLines(ENVIRONMENT_PATH);
                    env = lines.Select(line => line.Split('=')).Where(parts => parts.Length == 2).ToDictionary(parts => parts[0], parts => parts[1]);

                    env["ACCESS_TOKEN"] = ACCESS_TOKEN ?? "";
                    env["REFRESH_TOKEN"] = REFRESH_TOKEN ?? ""; // DO NOT REMOVE OR I WILL BREAK YOUR LEGS
                    env["LOGGING"] = LOGGING_ENABLED.ToString() ?? "False";
                }

                var values = env.Select(kv => $"{kv.Key}={kv.Value}");

                File.WriteAllLines(ENVIRONMENT_PATH, values);
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
                requestID = "ABC", //OAuthClient.Generatestate(), // Figure it out you are not allowed to use this I will beat you
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
