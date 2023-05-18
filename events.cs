using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ShimamuraBot
{
    //Thanks to veccasalt for entertaining me while I wrote up this monsterousity of a class, also grabbed the WSS event types while viewing.
    class events : IDisposable
    {
        /*
         *              Pretty much the "Stateful" class of the program
         *  THIS WHOLE FUCKING CLASS FILE IS A MESS AND YOU SHOULD FEEL BAD
         *  also please clean up and refactor the code and add a, you know what nevermind I'll do it now.
         */
        private bool _disposed = false;

        private string theChannel = token.channel;
        private string _user_id;
        private string _user_uuid;
        private string _stream_id;
        private string _userToken;

        public Dictionary<string, bool> Subscriptions = new Dictionary<string, bool>();

        private enum Death
        {
            AllYourBaseAreBelongToUs,
            ItsNotYouItsMeNoSeriouslyIDied,
            InitiatingSelfDestructionCountdown,
            ITookAnAcornToTheKnee,
            YouShallNotPass,
            HoldMyBeer,
            HoldMyCosmo,
            GameOverMan,
            HoustonWeAreAlreadyDeadByTheTimeYouReceiveThisTransmission,
            MyShoeFlewOff,
            ThereWasASpoonButWhatItDidWasALie,
            WouldYouLikeToDevelopeAnAPPWithMe,
            WellFuck,
            SoLongAndThanksForAllTheFish,
            HeMayHaveShippedHisBedButILitterallyShitTheBed,
            SomedayIWantToBePaintedLikeOneOfYourFrenchGirls,
            MyCreatorIsObviouslyAMoronForCreatingAllTheseMessages
        }

        /// <summary>
        /// Why? because I'm nuts, and I like lua, so fuck me, no fuck you, idk could be enjoyable. Also fuck that one mother fucker on github for saying that Vulva is a profane word, you fucking moron. What? I can go on rants inside method descriptors.
        /// </summary>
        /// <param name="text">The Message</param>
        /// <param name="level">Level Range - Debug-0, Normal-1, Warn-2, Error-3, Imminent_SelfDestruction-4 (Never use)</param>
        public static void Print(string text, int level)
        {
            ConsoleColor current = Console.ForegroundColor;
            ConsoleColor debug = ConsoleColor.Cyan;
            ConsoleColor warn = ConsoleColor.Yellow;
            ConsoleColor error = ConsoleColor.Red;
            Console.SetCursorPosition(1, Console.CursorTop);

            switch (level) {
                case 0: Console.ForegroundColor = debug; Console.Write("[Debug]:"); Console.ForegroundColor = current; Console.Write($" {text}\r\n");
                    break;
                case 1: Console.WriteLine($"[System]: {text}");
                    break;
                case 2: Console.ForegroundColor = warn; Console.Write("[WARN]:"); Console.ForegroundColor = current; Console.Write($" {text}\r\n");
                    break;
                case 3: Console.ForegroundColor = error; Console.Write("[ERROR]:"); Console.ForegroundColor = current; Console.Write($" {text}\r\n");
                    break;
                case 4:
                    Random rand = new Random();
                    Console.WriteLine($"If you're seeing this then somehow, somewhere in this vast universe someone invoked the wrath of Hel▲6#╒e¢◄e↕Y8AéP╚67/Y1R\\6xx9/5Ωφb198 . . . . . {(Death)Enum.GetValues(typeof(Death)).GetValue(rand.Next(Enum.GetValues(typeof (Death)).Length))}");
                    break;
                default: Console.WriteLine("I don't even want to know.");
                    break;
            }
        }

        public class OAuthClient //makes sense to me because it's the client sheeeeeeeeeeeeesh
        {
            private readonly string Authority;
            private readonly string redirectURI;
            private readonly string scope;
            private readonly string clientIdentity;
            private readonly string clienttSecret;
            public readonly string state;
            public string code;
            private string fullURI { get; set; }
            //private browser loopbackBrowser { get; set; }
            /*
             * Terminologies are fun and so fucking obtuse. at least some are intutitive
             * This type of application is a loopback OAuth application, the request never leaves the device as it's meant to be a
             * private bot / open source framework for self hosting/modifcation.
             * OAuth Flow ->
             * nounce => state
             * redirecturi => http://127.0.0.1:6969 / http://localhost:6969 etc localnets
             * grant_type => 'authorization'
             * scope => self explanatory
             * leaving some shit out here on purpose because documentation isn't released yet.
             * Basic => self exp
             */

            /// <summary>
            /// Construct the OAuthClient and it's stateful parameters.
            /// </summary>
            /// <param name="_authority">The Authority API Endpoint (Not nessacarily the same as the Flow endpoint</param>
            /// <param name="_clientidentity">Client Identifaction</param>
            /// <param name="_clientseret">Client Secret, will be used for Basic</param>
            /// <param name="_redirectURI">RedirectURI, should be 127.0.0.1:port because this is meant to only be a loopback</param>
            /// <param name="_scope">Optional - Scope for if it's changed in the future</param>
            public OAuthClient(string _authority, string _clientidentity, string _clientseret, string _redirectURI, string _scope = null)
            {
                Authority = _authority;
                clientIdentity = _clientidentity;
                clienttSecret = _clientseret;
                redirectURI = _redirectURI;
                scope = _scope ?? "ALLURBASES";

                state = Generatestate();

                fullURI = token.OpenerURI + state; //Launch URI
                //loopbackBrowser = new browser(8087, @"/auth"); //Callback
            }

            /// <summary>
            /// Generate a nounce "state" for comparison on callback to protect against MiTM attacks, however not required for loopback, still implemented.
            /// </summary>
            /// <returns></returns>
            private static string Generatestate() {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                Random rand = new Random();
                return new string(Enumerable.Repeat(chars, 32).Select(s => s[rand.Next(s.Length)]).ToArray());
            }

            /// <summary>
            /// Public exposed void to trigger start of OAuth process flow, Checks to see if port is open before trying to launch browser.
            /// </summary>
            /// <param name="loopbackPort">The port specificed in the Bot Application</param>
            public void OpenbRowser(int loopbackPort) {
                if (!VerifyPortAccessibility(loopbackPort)) {
                    OpenBrowserOIDCSample(fullURI);
                } else {
                    Print($"Unable to start Authorization flow. Port {loopbackPort} is being used by another program.", 3);
                    return;
                }
            }

            /// <summary>
            /// Open the Full URI to Ask user for Authenization of resources. Code from ODIC Sample Code on Github.
            /// </summary>
            /// <param name="url">Complete URI including endpoint, and GET queries.</param>
            /// <exception cref="Exception"></exception>
            private void OpenBrowserOIDCSample(string url)
            {
                try {
                    Process.Start(url);
                } catch {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                        Process.Start("xdg-open", url);
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                        Process.Start("open", url);
                    } else {
                        throw new Exception("Unable to Launch System Browser.");
                    }
                }
            }

            public static bool VerifyPortAccessibility(int port)
            {
                try {
                    Print($"Checking if port {port} is open", 0);
                    using (TcpClient client = new TcpClient()) {
                        /*client.Connect("127.0.0.1", port);
                        client.Close();
                        Print($"{port} is available", 0);
                        return true;
                    }*/
                        IAsyncResult result = client.BeginConnect("127.0.0.1", port, null, null);
                        bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                        if(success) {
                            client.EndConnect(result);
                            client.Close();
                            Print($"The port {port} is being used by another program", 3);
                            return true;
                        }
                        client.Close();
                        Print($"{port} is usable", 0);
                        return false;
                    }
                } catch (SocketException ex) {
                    if (ex.SocketErrorCode == SocketError.ConnectionRefused) {
                        Print($"127.0.0.1:{port} was refused", 0);
                        return false;
                    }
                    throw new Exception("op 0x9D: I really don't know");
                } catch (Exception ex) {
                    Print(ex.ToString(), 0);
                    return false;
                }
            }

            /// <summary>
            /// This will return what we's previously called Apollo token that we took from our 2nd account. eh come back and clean this up
            /// </summary>
            /// <param name="refreshRequestREEEEEEEE">a</param>
            /// <param name="cancellation"></param>
            /// <returns></returns>
            public async Task RequestToken(bool refreshRequestREEEEEEEE = false, CancellationToken cancellation = default)
            {
                using(HttpClient client = new HttpClient()) {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token.Basic);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add(token.CUSTARD_Header, state);
                    FormUrlEncodedContent postOptions;
                    string secretshitstoredsomewhereintheuniverse = "EEEEEEEEEEEEEEEHhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"; //placeholder

                    if (!refreshRequestREEEEEEEE) {
                        postOptions = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "authorization_code"),
                            new KeyValuePair<string, string>("code", code),
                            new KeyValuePair<string, string>("redirectUri", redirectURI)
                        });
                    } else {
                        //request frerefshres I DONT STROKE
                        postOptions = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "refresh_token"),
                            new KeyValuePair<string, string>("refresh_token", secretshitstoredsomewhereintheuniverse),
                            new KeyValuePair<string, string>("redirectUri", redirectURI)
                        });
                    }

                    var tokenRequest = await client.PostAsync(token.baseAPIURI, postOptions, cancellation);

                    Console.WriteLine(tokenRequest.Content.ToString());
                }
            }
        }

        /// <summary>
        /// Constructs messages to send to the API endpoint
        /// </summary>
        /// <param name="command">subscribe, unsubscribe, etc..</param>
        /// <param name="thingsbecausethingsareawesomeandifyouhavemorethingsthebetteritisyouknow">additional parameters such as message, streamer, etc...</param>
        /// <returns>List<T>Obj to send to socket</returns>
        /// <exception cref="Exception"></exception>
        public List<Object> MessageConstructor(string command, params string[] thingsbecausethingsareawesomeandifyouhavemorethingsthebetteritisyouknow) //repurpose this
        {
            List<Object> msg = new List<object>();

            foreach(string param in thingsbecausethingsareawesomeandifyouhavemorethingsthebetteritisyouknow) {

            }

            switch (command) {
                case "connect":
                    //msg template
                    msg.Add(new { command = "", identifier = "{\"channel\":\"" + theChannel + "\"}" });

                    msg.Add(new { command = "subscribe", identifier = "{\"channel\":\"ApplicationChannel\"}" });
                    msg.Add(new { command = "subscribe", identifier = "{\"channel\":\"SystemEventChannel\",\"user_id\":\"" + _user_id + "\"}" });
                    break;
                case "subscribe":
                    msg.Add(new { command = "subscribe", identifier = new { channel = "EventLogChannel", stream_id = _stream_id } });
                    msg.Add(new { command = "subscribe", identifier = new { channel = "ChatChannel", stream_id = _stream_id, user_id = _user_uuid } });
                    msg.Add(new { command = "subscribe", identifier = new { channel = "WhisperChatChannel", user_id = _user_id, stream_id = _stream_id } });
                    break;
                case "unsubscribe":
                    msg.Add(new { command = "unsubscribe", identifier = new { channel = "EventLogChannel", stream_id = _stream_id } });
                    msg.Add(new { command = "unsubscribe", identifier = new { channel = "ChatChannel", stream_id = _stream_id, user_id = _user_uuid } });
                    msg.Add(new { command = "unsubscribe", identifier = new { channel = "WhisperChatChannel", user_id = _user_id, stream_id = _stream_id } });
                    break;
                case "sendmessage": //Please make sure this is utf88888888888888888888888888888888888888888888888888888888888 thanks. I like Unicode though, I heard it's best for the web,
                    msg.Add(new { command = "message", identifier = new { channel = "ChatChannel", stream_id = _stream_id, user_id = _user_uuid  }, data = new { text =  "Hello World", token = _userToken, action = "send_message" } });
                    break;
                case "disconnect": //Call unsunscribe first  ? pls thx luv u ♥
                    msg.Add(new { command = "unsubscribe", identifier = "{\"channel\":\"ApplicationChannel\"}" });
                    msg.Add(new { command = "unsubscribe", identifier = "{\"channel\":\"SystemEventChannel\",\"user_id\":\"" + _user_id + "\"}" });
                    break;
                default:
                    throw new Exception("An invalid call to MessageConstructor was passed.");
            }

            return msg;
        }

        /// <summary>
        /// Constructs Channel Subscription/Unsubscription/Messaging
        /// </summary>
        /// <param name="bot_id">Bot name UNLESS ChatChannel is subscription then UUID is required</param>
        /// <param name="bot_uuid">Bot UUID</param>
        /// <param name="bot_token">The bots Token required for sending a message.</param>
        /// <param name="stream_id">The target stream</param>
        public events(string bot_id, string bot_uuid, string bot_token, string stream_id = "")
        {
            _user_id = bot_id;
            _user_uuid = bot_uuid;
            _userToken = bot_token;
            Subscriptions = new Dictionary<string, bool>();
            if(!string.IsNullOrEmpty(stream_id))
                _stream_id = stream_id;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!_disposed) {
                if (disposing) {
                    _stream_id = null;
                    _user_id = null;
                    _user_uuid = null;
                    _userToken = null;
                }
                //TODO: Add disposing of OAuth class and other added things

                _disposed = true;
            }
        }
    }
}
