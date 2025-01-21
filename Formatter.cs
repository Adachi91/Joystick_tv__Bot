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
            private static string name = "Format:JWT";
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

            private static validation? _WebObject { get; set; }
            private static string? _Token { get; set; } = null;

              ///==============================================================================\\\
             ///  A dumbstructor is what I call a non-constructor, acting like a constructor.   \\\
            /// ================================================================================ \\\

            /// <summary>
            ///  Checks if token hasn't expired (12Hr offset)
            /// </summary>
            /// <returns><see cref="bool"/> is expired</returns>
            public static bool Expired => ((_WebObject?.expiry ?? 0) - GetUnixTimestamp() <= 43200);
            /// <summary>
            ///  Checks if a Global JWT Token exists
            /// </summary>
            /// <returns><see cref="bool"/> if valid token is held</returns>
            public static bool Valid => !string.IsNullOrEmpty(ACCESS_TOKEN) && _WebObject != null;


            /// <summary>
            ///  _Dumbstructor: Parses token if is held, and not parsed already or is expired and needs to be parsed again.
            /// </summary>
            /// <returns>Bool - Success</returns>
            public static async Task<bool> Token() {
                if (DEBUGGING_ENABLED) Print(name, $"Attempting to parse web token.", PrintSeverity.Debug);
                if (string.IsNullOrEmpty(ACCESS_TOKEN)) { if (DEBUGGING_ENABLED) Print(name, $"ACCESS_TOKEN IS EMPTY", PrintSeverity.Debug); return false; } // Short-Circuit - OAuth flow needs to happen, no token is held.

                
                try { await JWT.Parse(ACCESS_TOKEN); if (DEBUGGING_ENABLED) Print(name, $"Web Token succesfully stored.", PrintSeverity.Debug); return true; } catch { return false; }
            }

            public static int? GetExpiration => _WebObject != null ? _WebObject.expiry : null;
            public static int? GetNotBefore => _WebObject != null ? _WebObject.not_before : null;
            public static int? GetIssuedTime => _WebObject != null ? _WebObject.issued_at : null;
            public static string GetChannelIdentifier => _WebObject?.channel_id ?? null!;
            public static string GetBotIdentifier => _WebObject?.bot_id ?? null!;

            /// <summary>
            ///  Parse a JSON Web Token and extract Payload.
            /// </summary>
            /// <param name="token">String - JWT</param>
            /// <exception cref="BotException"></exception>
            private static Task Parse(string token) {
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
                } catch {
                    throw new BotException(name, "Could not validate the JWT Payload.");
                }
                return Task.CompletedTask;
            }
        }
        #endregion

        #region Print Functionality
        private static SemaphoreSlim STOPEATINGSHIT = new(1, 1);

        private static object formatPrint(string sender, string txt, PrintSeverity lvl) //TODO: Start random text strings to make sure it can handle []: tagging like "Hi [where] Are you [rom you there?"
        {
            string Blah;
            string leveltxt = "";
            switch((short)lvl) { case 0: leveltxt = "[Debug]"; break; case 2: leveltxt = "[Warning]"; break; case 3: leveltxt = "[Error]"; break;  }

            dynamic holder = new {
                Name = Blah = sender.ToLower() switch { "chat" => $"[{DateTime.Now:HH:mm:ss}] ", "nt" => $"[{DateTime.Now:HH:mm:ss}]: ", "" => "", _ => $"[{DateTime.Now:HH:mm:ss}]{leveltxt}[{sender}]: " },
                Message = txt
            };

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
        /// <param name="sender"><see cref="string"/> The sender name.<para>Usage:<br />Sender,<br />Chat - Chat Format,<br />NT - No Tag, <b>with</b> DateTime<br />string.empty - No Tag, <b>No</b> DateTime</para></param>
        /// <param name="text"><see cref="string"/> Message body</param>
        /// <param name="level"><see cref="PrintSeverity"/> Error level.</param>
        public static void Print(string sender, string text, PrintSeverity level) { //https://en.wikipedia.org/wiki/ANSI_escape_code
            STOPEATINGSHIT.Wait(); // Stop eating CHARACTERS.
            ConsoleColor current = Console.ForegroundColor;
            ConsoleColor debug = ConsoleColor.Cyan;
            ConsoleColor warn = ConsoleColor.Yellow;
            ConsoleColor error = ConsoleColor.Red;
            Console.SetCursorPosition(0, Console.CursorTop); //I think I need to watch this. it might be overwriting user input; (Yes.)
            dynamic ctx = formatPrint(sender, text, level);
            //Console.CursorLeft = 0;

            switch ((short)level) {
                case 0:
                    //#if DEBUG
                    if (!DEBUGGING_ENABLED) break;
                    Console.ForegroundColor = debug; Console.Write($" {ctx.Name}"); Console.ForegroundColor = current; Console.Write($"{ctx.Message}{Environment.NewLine}");
                    _ = Logger.Log("Debug", new string[] { $"[Component:{sender}]:", $"{ctx.Message}" });
                    //#endif
                    break;
                case 1: /*int cl = Console.WindowWidth - ($" {ctx.Name}{ctx.Message}").Length;*/ Console.WriteLine($" {ctx.Name}{ctx.Message}" /*+ (cl > 0 ? new string(' ', cl) : "")*/);  //if (cl > 0) Console.Write(new string('|', cl));
                    break;
                case 2: Console.ForegroundColor = warn; Console.Write($" {ctx.Name}"); Console.ForegroundColor = current; Console.Write($"{ctx.Message}{Environment.NewLine}");
                    break;
                case 3: /*Console.Write($" \x1B[38;5;9m[ERROR]{ctx[0]}: {ctx[1]}\x1B[38;5;15m{Environment.NewLine}"); test to switch to ANSI escape, good idea? great? or horrible.. */
                    Console.ForegroundColor = error; Console.Write($" {ctx.Name}"); Console.ForegroundColor = current; Console.Write($"{ctx.Message}{Environment.NewLine}");
                    if (sender != "Logger") // Prevent recursion. BotException -> Print(Error) -> Logger -> BotException -> Print(Error) -> Logger
                        _ = Logger.Log("ERROR", new string[] { $"[Component:{sender}]:", $"{ctx.Message}" });
                    break;
                default: Console.WriteLine($"I don't even want to know. Offender: {sender}");
                    break;
            }
            // Said what you had to say.

            /// https://learn.microsoft.com/en-us/windows/console/console-virtual-terminal-sequences

            //Console.Write($">{UserInput.ToString()}");
            Console.Write($"{USERNAME ?? "$"}>");
            STOPEATINGSHIT.Release();
        }
        #endregion


        /// <summary>
        ///  Get the current UTC Unix Timestamp
        /// </summary>
        /// <returns><see cref="long"/> Timestamp</returns>
        public static long GetUnixTimestamp() {
            return (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
