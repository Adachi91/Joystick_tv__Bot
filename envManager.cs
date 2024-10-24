using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace ShimamuraBot
{
    public static class envManager
    {
        public static Dictionary<string, string> audioModule = new Dictionary<string, string>();
        public static Dictionary<string, string> vtuberModule = new Dictionary<string, string>();

        /// <summary>
        ///  Loads the environment file
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void load() {
            string tmp = null; //hold the long in a string for this type of switch to work.
            string _logging = null;
            string _vnyan_hook = null;
            string _configPth = null;

            foreach (var line in File.ReadAllLines(ENVIRONMENT_PATH)) {
                if (line.StartsWith("#")) continue;
                var split = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length != 2) continue;

                //Environment.SetEnvironmentVariable(split[0], split[1]);
                //I decided not to use Enviroment Variables because it isn't imediately clear via code what the variable names are.

                var envKey = split[0] switch {
                    "HOST" => HOST = split[1],
                    "CLIENT_ID" => CLIENT_ID = split[1],
                    "CLIENT_SECRET" => CLIENT_SECRET = split[1],
                    "WSS_HOST" => WSS_HOST = split[1],
                    "JWT" => APP_JWT = split[1],
                    "JWT_REFRESH" => APP_JWT_REFRESH = split[1],
                    "JWT_EXPIRE" => tmp = split[1], //TODO extract from JWT and delete this entry.
                    "CHANNELGUID" => CHANNELGUID = split[1],
                    
                    "LOGGING" => _logging = split[1],
                    "DISCORDHOOK" => DISCORD_URI = split[1],
                    "CONFIG" => _configPth = split[1],
                    "VNYAN" => _vnyan_hook = split[1], 
                    _ => throw new BotException("Environment", $"The Enviroment Keys in are not structured properly in the .env file{Environment.NewLine}The minimum is required{Environment.NewLine}HOST=HOST_URL{Environment.NewLine}CLIENT_ID=YOUR_CLIENT_ID{Environment.NewLine}CLIENT_SECRET=YOUR_CLIENT_SECRET{Environment.NewLine}WSS_HOST=THE_WSS_ENDPOINT{Environment.NewLine}")
                };
            }

            if (HOST == null || CLIENT_ID == null || CLIENT_SECRET == null || WSS_HOST == null) throw new Exception($"One or more values in the environment file was not found{Environment.NewLine}The minimum is required{Environment.NewLine}HOST=HOST_URL{Environment.NewLine}CLIENT_ID=YOUR_CLIENT_ID{Environment.NewLine}CLIENT_SECRET=YOUR_CLIENT_SECRET{Environment.NewLine}WSS_HOST=THE_WSS_ENDPOINT{Environment.NewLine}");

            ACCESS_TOKEN = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));
            WSS_GATEWAY = $"{WSS_HOST}?token={ACCESS_TOKEN}"; //not anymore :) //this needs to be set where JWT is handled.
            if (!string.IsNullOrEmpty(tmp)) try { APP_JWT_EXPIRY = Convert.ToInt64(tmp); } catch { /* write out APP_JWT_EXPIRY */ new BotException("Environment-Loader", "Unable to convert APP_JWT_EXPIRY to long."); }
            if (!string.IsNullOrEmpty(_logging)) try { LOGGING_ENABLED = Convert.ToBoolean(_logging); } catch { LOGGING_ENABLED = false; new BotException("Environment-Loader", "LOGGING Variable is not a valid value. Defaulting to False. (Valid opt: True, False)"); }
            if (!string.IsNullOrEmpty(_vnyan_hook) && Convert.ToBoolean(_vnyan_hook) == true) vNyan = new VNyan();

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
        public static void write(bool _defaults = false)
        {
            Dictionary<string, string> env;

            if(_defaults) {
                env = new Dictionary<string, string> {
                    ["HOST"] = "https://example.net",
                    ["CLIENT_ID"] = "YOUR_CLIENT_ID",
                    ["CLIENT_SECRET"] = "YOUR_CLIENT_SECRET",
                    ["WSS_HOST"] = "WSS_ENDPOINT",
                };
            } else {
                string[] lines = File.ReadAllLines(ENVIRONMENT_PATH);
                env = lines.Select(line => line.Split('=')).Where(parts => parts.Length == 2).ToDictionary(parts => parts[0], parts => parts[1]);

                env["JWT"] = APP_JWT ?? "";
                env["JWT_REFRESH"] = APP_JWT_REFRESH ?? "";
                env["JWT_EXPIRE"] = APP_JWT_EXPIRY.ToString() ?? "";
                env["LOGGING"] = LOGGING_ENABLED.ToString();
                env["CHANNELGUID"] = CHANNELGUID ?? "";
            }

            var values = env.Select(kv => $"{kv.Key}={kv.Value}");

            File.WriteAllLines(ENVIRONMENT_PATH, values);
        }


        public static void updateKey(string key, string value) {
            List<string> fileLines = File.ReadAllLines(ENVIRONMENT_PATH).ToList();
            bool _updated = false;

            for(int i = 0; i < fileLines.Count; i++) {
                if (fileLines[i].StartsWith($"{key}=")) {
                    fileLines[i] = $"{key}={value}";
                    _updated = true;
                    break;
                }
            }

            if(!_updated)
                fileLines.Add($"{key}={value}");
            File.WriteAllLines(ENVIRONMENT_PATH, fileLines);
            Print($"[Environment]: {key} was updated", 0);
        }


        public class Modules
        {
            private string path { get; set; }
            private string name { get; set; }
            private string command { get; set; }
            private string application { get; set; }
            private int value { get; set; }

            private object VTS_Hotkey = new {
                apiName = "VTubeStudioPublicAPI",
                apiVersion = "1.0",
                requestID = OAuthClient.Generatestate(),
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
                Print($"[ModuleLoader]: Could not find the file specified", 3);
            }
        }
    }
}
