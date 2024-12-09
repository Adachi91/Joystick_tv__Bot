using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
//using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
//using System.Reflection;

namespace ShimamuraBot.Modules
{
    internal class ModuleLoader
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static string name = "ModuleLoader";


        #region JSONStruct
        public class appData {
            [JsonPropertyName("type"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string Type { get; set; }

            [JsonPropertyName("name"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string Name { get; set; }

            [JsonPropertyName("cost"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public int? Cost { get; set; }
        }

        public class ApplicationSettings {
            [JsonPropertyName("module"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string Module { get; set; }

            [JsonPropertyName("data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public appData Data { get; set; }
        }
        #endregion

        private static Dictionary<string, object> _Sounds { get; set; } //only used locally
        private static Dictionary<string, object> _Redeems { get; set; } //external/local
        private static Dictionary<string, object> _Modules { get; set; }
        private static Dictionary<string, object> _vNyan { get; set; }
        private static Dictionary<string, object> _VTS { get; set; }


        //TODO: You handle vnyan spawning
        //TODO: You handle VTS spawning
        //TODO: You handle native on start

        public static string SaveSettings()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var settings = new Dictionary<string, object>
            {
                ["sounds"] = _Sounds,
                ["redeems"] = _Redeems,
                ["native"] = _Modules,
                ["vnyan"] = _vNyan,
                ["vts"] = _VTS
            };

            return JsonSerializer.Serialize(settings, options);
        }



        public static async Task LoadSettings(bool writer = false, string json = "")
        {
            await _semaphore.WaitAsync();
            try {
                if (!File.Exists("settings.json"))
                    await File.WriteAllTextAsync("settings.json", "{}");

                var settingFileLines = await File.ReadAllTextAsync("settings.json");

                using(JsonDocument docu = JsonDocument.Parse(settingFileLines)) {
                    JsonElement root = docu.RootElement;

                    if(root.TryGetProperty("modules", out var module)) {
                        _Modules = JsonSerializer.Deserialize<Dictionary<string, object>>(module);
                    }


                    if(root.TryGetProperty("sounds", out var sound)) {
                        _Sounds = JsonSerializer.Deserialize<Dictionary<string, object>>(sound);
                    }

                    if(root.TryGetProperty("redeems", out var redeems)) {
                        _Redeems = JsonSerializer.Deserialize<Dictionary<string, object>>(redeems);
                    }
                }

            } catch (Exception ex) {
                new BotException("ModuleLoader", "Fucking exploded it did, I saw it, I did. ", ex);
            } finally {
                _semaphore.Release();
                Print(name, $"Semaphore released!", PrintSeverity.Debug);
            }
            Print(name, $"Goodbye.", PrintSeverity.Debug);
        }

        /// <summary>
        ///  Get the current application settings.
        /// </summary>
        /// <returns>ApplcationSettings class - Note: Can be null if an error was raised</returns>
        public static async Task<ApplicationSettings> GetSettings() =>
            await SettingsHandler(false);


        /// <summary>
        ///  Writes to the settings file
        /// </summary>
        /// <param name="module">Target app</param>
        /// <param name="type">I can't even remember</param>
        /// <param name="name">redeem/internal name</param>
        /// <param name="cost">redeem cost/internal wujt</param>
        /// <returns>zeroZEROTWO</returns>
        public static async Task WriteSettings(string module, string type, string name, string cost) =>
            await SettingsHandler(true, new string[] { module, type, name, cost });

        // M8 wat kind of bullshit did you do - TODO: Fix this, it's a mess do not send a string[] of parameters send them all or none   or an object that you can't Key check fml
        private static async Task<ApplicationSettings> SettingsHandler(bool writing, params string[] dparams) { // string module, string type, string name, int? cost) {
            ApplicationSettings appset = null;

            int cost = 0;
            string name = string.Empty;
            string type = string.Empty;
            string module = string.Empty;

            if (dparams.Length > 0 && writing) {
                try {
                    cost = Convert.ToInt32(dparams[3]);
                    if (string.IsNullOrEmpty(dparams[0]) || string.IsNullOrEmpty(dparams[1]) || string.IsNullOrEmpty(dparams[2]))
                        throw new ArgumentException("Params can not be empty while writing.");
                    name = dparams[2];
                    type = dparams[1];
                    module = dparams[0];
                } catch (FormatException fmtex) {
                    new BotException("ModuleLoader", "Tried to parse a non-int", fmtex);
                    return appset;
                } catch (Exception ex) {
                    new BotException("ModuleLoader", "Unhandled exception :: ", ex);
                }
            }

            await _semaphore.WaitAsync();
            try
            {
                if (!File.Exists("settings.json"))
                    await File.WriteAllTextAsync("settings.json", "[]");

                var settingFileLines = await File.ReadAllTextAsync("settings.json");
                //var rewardList = deserialize
                var listofSettings = JsonSerializer.Deserialize<List<ApplicationSettings>>(settingFileLines) ?? new List<ApplicationSettings>();
                
                var setting = listofSettings.FirstOrDefault(setting => setting.Data.Name == name);

                if (writing) {//Write new values
                    //listofSettings[setting].Module = module;
                    appset.Module = module;
                    appset.Data = new appData() { Type = type, Name = name, Cost = cost };
                }


            }
            catch (Exception ex)
            {
                new BotException("Settings", "Unhandled exception :: ", ex);
            }
            finally
            {
                _semaphore.Release();
            }

            return appset;
        }

    }
}
