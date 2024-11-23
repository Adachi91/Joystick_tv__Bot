using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShimamuraBot
{
    internal class Formatter
    {

        #region JWT_Parsing
        /// <summary>
        ///  Why? Because I hate libraries, and myself.
        /// </summary>
        public class JWT
        {
            /// Let's define goals for JWT Class.
            // I want my cake and eat it too, if I do this I will have to write a lot of logic to keep from returning null values.
            // What I mean by this is I want JWT Class to handle all token related things including long storing the values
            // Having return methods to get those values so any part of the program can be like Hey JWT when does token expire?
            // _ OR _ I can just make it Parse a token, and return a value every time
            // This also requires some care in the logic because there is a possible null return type
            // That is IF ACCESS_TOKEN is not defined yet.
            // So choose your path wisely and make sure to upstream check interactions.

            // To help here are some current things this will leave a gap in
            // No more global JWT_ accessors loaded from environment.
            private static string name = "Format.JWT";
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
            private class validation
#pragma warning restore CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.
            {
                [JsonPropertyName("exp")]
                public int? expiry { get; set; }// : 1731714452,
                [JsonPropertyName("nbf")]
                public int? not_before { get; set; }//"nbf": 1730850452,
                [JsonPropertyName("iat")]
                public int? issued_at { get; set; }//"iat": 1730850452,
                [JsonPropertyName("aud")]
                public string audience { get; set; }//"aud": "application",
                public string bot_id { get; set; }//"bot_id": "",
                public string channel_id { get; set; }//"channel_id": ""
            }

            private static validation _WebObject { get; set; } = null;
            private static string _Token { get; set; } = null;

              ///==============================================================================\\\
             ///  A dumbstructor is what I call a non-constructor, acting like a constructor.   \\\
            /// ================================================================================ \\\

            /// <summary>
            ///  Checks if token hasn't expired (12Hr offset)
            /// </summary>
            /// <returns>Boolean - True:Expired, False:Valid</returns>
            public static bool Expired => (_WebObject.expiry - GetUnixTimestamp() <= 43200);
            /// <summary>
            ///  Checks if a Global JWT Token exists
            /// </summary>
            /// <returns>Boolean</returns>
            public static bool Valid => !string.IsNullOrEmpty(ACCESS_TOKEN) && IsHazValue; // Added _WObj check, I figured if I went through and typed up conditionals this would be the most used.
            /// <summary>
            ///  Checks if JWT WebObject has been _Dumbstructed
            /// </summary>
            /// <returns>Boolean</returns>
            private static bool IsHazValue => _WebObject != null;


            /// <summary>
            ///  _Dumbstructor: Parses token if is held, and not parsed already or is expired and needs to be parsed again.
            /// </summary>
            /// <returns>Bool - Success</returns>
            public static async Task<bool> Token() {
                if(string.IsNullOrEmpty(ACCESS_TOKEN)) return false; // Short-Circuit - OAuth flow needs to happen, no token is held.

                //if(Expired) {
                    try { await JWT.Parse(ACCESS_TOKEN); return true; } catch { return false; }
                //}

                //return true;
            }

            public static int? GetExpiration => JWT.IsHazValue ? _WebObject.expiry : null;//!Expired() ? (int)_WebObject.expiry : null;
            public static int? GetNotBefore => JWT.IsHazValue ? _WebObject.not_before : null;
            public static int? GetIssuedTime => JWT.IsHazValue ? _WebObject.issued_at : null;
            public static string GetChannelIdentifier => _WebObject.channel_id ?? null; //!Expired() ? _WebObject.channel_id : null;
            public static string GetBotIdentifier => _WebObject.bot_id ?? null;

            /// <summary>
            ///  Parse a JSON Web Token and extract Payload.
            /// </summary>
            /// <param name="token">String - JWT</param>
            /// <exception cref="BotException"></exception>
            private static Task Parse(string token) {
                //if (string.IsNullOrEmpty(ACCESS_TOKEN)) return null;
                //if ((_WebObject == null || JWT.Expired) && _Token != token) return null;

                string[] parts = token.Split('.');
                if(parts.Length != 3) throw new BotException(name, "Invalid Web Token Format.");

                string payload = parts[1]; // extract payload (header . payload . signatory)
                payload = payload.Replace('-', '+').Replace('_', '/');

                ///https://datatracker.ietf.org/doc/html/rfc7515#section-2 return any missing padding.
                switch (payload.Length % 4) {
                    case 2: payload += "=="; break;
                    case 3: payload += "="; break;
                }

                string convert = Encoding.UTF8.GetString(Convert.FromBase64String(payload));

                // why do mornings suck so muchhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh
                try {
                    _WebObject = JsonSerializer.Deserialize<validation>(convert);
                    //Print(name, $"JWT DEBUG ===>\r\nexp:{_WebObject.expiry}\r\nnbf:{_WebObject.not_before}\r\niat:{_WebObject.issued_at}\r\naud:{_WebObject.audience}\r\nbotid:{_WebObject.bot_id}\r\nchannel:{_WebObject.channel_id}\r\nEND DEBUG <=========", PrintSeverity.Debug);
                    //return validate;
                } catch {
                    throw new BotException(name, "Could not validate the JWT Payload.");
                }
                return Task.CompletedTask; //idk I'm too stupid to care right now why this is required. I'll read later
            }
        }
        #endregion

        #region Print Functionality
        private static object formatPrint(string sender, string txt, PrintSeverity lvl) //TODO: Run random text strings to make sure it can handle []: tagging like "Hi [where] Are you [rom you there?"
        {
            string leveltxt = "";
            switch((short)lvl) { case 0: leveltxt = "[Debug]"; break; case 2: leveltxt = "[Warning]"; break; case 3: leveltxt = "[Error]"; break;  }

            dynamic holder = new {
                Name = sender == "Chat" ? $"[{DateTime.Now:HH:mm:ss}] " : $"[{DateTime.Now:HH:mm:ss}]{leveltxt}[{sender}]: ", // [19:33:22] Moo: Hey
                Message = txt
            };


            //Yeah I overdo shit
            /*int startIndex = txt.IndexOf('[');
            int endIndex = txt.IndexOf("]: ");
            string tag;

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex && startIndex == 0) {
                tag = txt.Substring(startIndex + 1, (endIndex - startIndex) - 1);
                holder[0] = $"[{tag}]";
                holder[1] = txt.Substring(endIndex + 3).Trim();
            } else {
                holder[1] = txt;
            }*/

            return holder;
        }

        public enum PrintSeverity : short {
            Debug = 0,
            Normal = 1,
            None = 1,
            Warn = 2,
            Error = 3,
            Chat = 4
        }

        /// <summary>
        /// Why? because I'm nuts, and I like lua, so fuck me, no fuck you, idk could be enjoyable. Also fuck that one mother fucker on github for saying that Vulva is a profane word, you fucking moron. What? I can go on rants inside method descriptors.
        /// </summary>
        /// <param name="text">The Message</param>
        /// <param name="level">PrintSeverity</param>
        public static void Print(string sender, string text, PrintSeverity level) { //https://en.wikipedia.org/wiki/ANSI_escape_code
            ConsoleColor current = Console.ForegroundColor;
            ConsoleColor debug = ConsoleColor.Cyan;
            ConsoleColor warn = ConsoleColor.Yellow;
            ConsoleColor error = ConsoleColor.Red;
            Console.SetCursorPosition(0, Console.CursorTop); //I think I need to watch this. it might be overwriting user input;
            dynamic ctx = formatPrint(sender, text, level); //index 0 is Tag from which class / service. index 1 is the message.

            switch ((short)level) {
                case 0:
                    #if DEBUG
                        Console.ForegroundColor = debug; Console.Write($" {ctx.Name}"); Console.ForegroundColor = current; Console.Write($"{ctx.Message}{Environment.NewLine}");
                        if (DEBUGGING_ENABLED) _ = Logger.Log("Debug", new string[] { $"[Component:{sender}]:", $"{ctx.Message}" });
                    #endif
                    break;
                case 1: Console.WriteLine($" {ctx.Name}{ctx.Message}");
                    break;
                case 2: Console.ForegroundColor = warn; Console.Write($" {ctx.Name}"); Console.ForegroundColor = current; Console.Write($"{ctx.Message}{Environment.NewLine}");
                    break;
                case 3: /*Console.Write($" \x1B[38;5;9m[ERROR]{ctx[0]}: {ctx[1]}\x1B[38;5;15m{Environment.NewLine}"); test to switch to ANSI escape, good idea? great? or horrible.. */
                    Console.ForegroundColor = error; Console.Write($" {ctx.Name}"); Console.ForegroundColor = current; Console.Write($"{ctx.Message}{Environment.NewLine}");
                    if (sender != "Logger") // Prevent recursion. BotException -> Print(Error) -> Logger -> BotException -> Print(Error) -> Logger
                        _ = Logger.Log("ERROR", new string[] { $"[Component:{sender}]:", $"{ctx.Message}" });
                    break;
                default: Console.WriteLine($"I don't even want to know.");
                    break;
            }
            Console.Write(">");
        }
        #endregion


        /// <summary>
        ///  Get the current UTC Unix Timestamp
        /// </summary>
        /// <returns>(long) Timestamp</returns>
        public static long GetUnixTimestamp() {
            return (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
