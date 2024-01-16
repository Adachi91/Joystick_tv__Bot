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
using System.Reflection.Metadata;
using static System.Net.Mime.MediaTypeNames;
using System.Buffers;

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

        #region Print Functionality
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

        private static string[] formatPrint(string txt)
        {
            string[] holder = { "", "" };
            //Yeah I overdo shit
            int startIndex = txt.IndexOf('[');
            int endIndex = txt.IndexOf("]: ");
            string tag;
            string ctx;

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex && startIndex == 0) {
                tag = txt.Substring(startIndex + 1, (endIndex - startIndex) - 1);
                holder[0] = $"[{tag}]";
                holder[1] = txt.Substring(endIndex + 3).Trim();
            } else {
                holder[0] = string.Empty;
                holder[1] = txt;
            }

            return holder;
        }

        /// <summary>
        /// Why? because I'm nuts, and I like lua, so fuck me, no fuck you, idk could be enjoyable. Also fuck that one mother fucker on github for saying that Vulva is a profane word, you fucking moron. What? I can go on rants inside method descriptors.
        /// </summary>
        /// <param name="text">The Message</param>
        /// <param name="level">0:Debug, 1:Normal, Warn:2, Error:3, UniversalHeatDeath:4</param>
        public static void Print(string text, int level)
        {
            ConsoleColor current = Console.ForegroundColor;
            ConsoleColor debug = ConsoleColor.Cyan;
            ConsoleColor warn = ConsoleColor.Yellow;
            ConsoleColor error = ConsoleColor.Red;
            Console.SetCursorPosition(1, Console.CursorTop);
            string[] ctx = formatPrint(text); //index 0 is Tag from which class / service. index 1 is the message.

            switch (level) {
                case 0: Console.ForegroundColor = debug; Console.Write($"[Debug]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n");
                    break;
                case 1: ctx = formatPrint(text); Console.WriteLine($"[System]{ctx[0]}: {ctx[1]}");
                    break;
                case 2: Console.ForegroundColor = warn; Console.Write($"[WARN]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n");
                    break;
                case 3: Console.ForegroundColor = error; Console.Write($"[ERROR]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n");
                    break;
                case 4:
                    Random rand = new Random();
                    Console.WriteLine($"If you're seeing this then somehow, somewhere in this vast universe someone invoked the wrath of Hel▲6#╒e¢◄e↕Y8AéP╚67/Y1R\\6xx9/5Ωφb198 . . . . . {(Death)Enum.GetValues(typeof(Death)).GetValue(rand.Next(Enum.GetValues(typeof (Death)).Length))}");
                    break;
                default: Console.WriteLine($"I don't even want to know.");
                    break;
            }
        }
        #endregion

        public static class TokenManager
        { //EVERYBODY GETS ACCESS OMG
            public static string Token;
            public static string refreshToken;
            public static string state;
            public static int expiry;


            /*public TokenManager(string _token, int _expiry, string _refresh, string _state) {
                Token = _token;
                refreshToken = _refresh;
                state = _state;
                expiry = _expiry;
            }*/

            public static void UpdateValues(string _token, int _expiry, string _refresh, string _state)
            {
                if (_state != state && !string.IsNullOrEmpty(state)) throw new Exception("states do not match");
                if (string.IsNullOrEmpty(state)) state = _state;

                Token = _token;
                refreshToken = _refresh;
                expiry = _expiry;
            }

            public static void CheckExpiry()
            {
                //is this dog?
            }

            //Load between sessions
            public static void LoadLocal(string ZeroZeroTwo)
            {
                //eh fuck it store it in a txt file named supersecret.txt

            }
        }

        public class OAuthClient
        {
            private readonly string Authority;
            private readonly string redirectURI;
            private readonly string scope;
            private readonly string clientIdentity;
            private readonly string clienttSecret;
            public readonly string state;
            public readonly string basicAuth;
            public readonly string header;
            public string fullURI { get; set; }
            public string code;
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
            /// <param name="_basic">The Basic auth key</param>
            /// <param name="_header">Custom Header to add to request</param>
            /// <param name="_scope">Optional - Scope for if it's changed in the future</param>
            public OAuthClient(string _authority, string _clientidentity, string _clientseret, string _redirectURI, string _basic, string _header, string _scope = null)
            {
                Authority = _authority;
                clientIdentity = _clientidentity;
                clienttSecret = _clientseret;
                redirectURI = _redirectURI;
                basicAuth = _basic;
                header = _header;
                scope = _scope ?? "ALLURBASES";

                state = Generatestate();

                fullURI = token.OpenerURI + state;
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
                    client.DefaultRequestHeaders.Add(token.customHeader, state);
                    FormUrlEncodedContent postOptions;
                    string secretshitstoredsomewhereintheuniverse = "EEEEEEEEEEEEEEEHhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"; //placeholder

                    if (!refreshRequestREEEEEEEE) {
                        postOptions = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "authorization_code"),
                            new KeyValuePair<string, string>("code", code), //TODO This doesn't make a god damn bit of sense it isn't init
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
