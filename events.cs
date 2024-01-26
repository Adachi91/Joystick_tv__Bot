using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
//using System.Net.Sockets;
using System.Text;
using System.IO;
//using System.Runtime.InteropServices;

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

        private string theChannel = $"{Program.GATEWAY_IDENTIFIER}"; //THIS NEEDS TO BE SERIALIZED! //token.channel;
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
                case 0:
                    #if DEBUG
                        Console.ForegroundColor = debug; Console.Write($"[Debug]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n"); 
                    #endif
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

        /*public class TokenManager
        { //EVERYBODY GETS ACCESS OMG
            public string Token;
            public string refreshToken;
            public string state;
            public int expiry;


            public TokenManager(string _token, int _expiry, string _refresh, string _state) {
                Token = _token;
                refreshToken = _refresh;
                state = _state;
                expiry = _expiry;
            }

            public void UpdateValues(string _token, int _expiry, string _refresh, string _state)
            {
                if (_state != state && !string.IsNullOrEmpty(state)) throw new Exception("states do not match");
                if (string.IsNullOrEmpty(state)) state = _state;

                Token = _token;
                refreshToken = _refresh;
                expiry = _expiry;
            }

            public void CheckExpiry()
            {
                //is this dog?
            }

            //Load between sessions
            public void LoadLocal(string ZeroZeroTwo)
            {
                //eh fuck it store it in a txt file named supersecret.txt

            }
        }*/

        public class OAuthClient
        {
            /// <summary>
            /// THIS IS THE ONMLY THING THE CLASS IS BEING USED FOR RIGHT NOW
            /// </summary>
            private readonly string Authority;
            private readonly Uri RedirectURI;
            private readonly string Scope;
            private readonly string ClientIdentity;
            private readonly string ClienttSecret;
            public readonly Uri Auth_URI;
            public readonly Uri Token_URI;
            public string State;
            public string OAuthCode;


            //public readonly string basicAuth;
            //public readonly string header;
            //public string fullURI { get; set; }
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


            /*public OAuthClient(string _authority, string _clientidentity, string _clientseret, string _redirectURI, string _basic, string _header, string _scope = null)
            {
                Authority = _authority;
                clientIdentity = _clientidentity;
                clienttSecret = _clientseret;
                redirectURI = _redirectURI;
                basicAuth = _basic;
                header = _header;
                scope = _scope ?? "ALLURBASES";

                state = Generatestate();

                fullURI = $"{Program.HOST}/api/oauth/authorize?client={clientIdentity}&scope=bot&state={state}"; 
                //fullURI = token.OpenerURI + state;
            }*/

            // Handle grant_type and code storage inside the class it doesn't nee dto be outside because it should all be handled by this class "THE" oauth constructor / class

            /// <summary>
            ///  OAuth Constructor class
            /// </summary>
            /// <param name="_host">The host with scheme</param>
            /// <param name="_client_id">Client Identifier</param>
            /// <param name="_client_secret">Client Secret</param>
            /// <param name="_authorize_uri">OAuth URI</param>
            /// <param name="_token_uri">Token URI</param>
            /// <param name="_redirect_uri">The redirect URI</param>
            /// <param name="_scope">Scope (optional) should be bot</param>
            public OAuthClient(string _host, string _client_id, string _client_secret, string _authorize_uri, string _token_uri, string _redirect_uri, string _scope = "ALLURBASES")
            {
                if(!_host.StartsWith("https://")) {
                    events.Print($"[OAuth]: Could not construct the OAuth class. The Scheme detected was not HTTPS", 3);
                    return;
                }

                Authority = _host;
                ClientIdentity = _client_id;
                ClienttSecret = _client_secret;
                RedirectURI = new Uri(_redirect_uri);

                Auth_URI = new Uri($"{_host}{_authorize_uri}");
                Token_URI = new Uri($"{_host}{_token_uri}");

                Scope = _scope;

            }


            /// <summary>
            /// Generate a nounce "state" for comparison on callback to protect against MiTM attacks, however not required for loopback, still implemented.
            /// </summary>
            /// <returns></returns>
            public static string Generatestate() {
                string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
                Random rand = new Random();
                return new string(Enumerable.Repeat(chars, 32).Select(s => s[rand.Next(s.Length)]).ToArray());
            }


            /// <summary>
            ///  Post to Joystick's API gateway and request a JWT
            /// </summary>
            /// <param name="type">Type of grant. 1:Request, 2:Renew</param>
            public async void callmewhateverlater(int type) {
                //Can I please get WebClient back ; _ ;  - 2nd attempt
                using (HttpClient hc = new HttpClient()) {
                    //oh boy here we go again
                    //First attempt

                    //over / under on it exploding?
                    using (var postMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(Authority + $"redirect_uri=unused&code={OAuthCode}&grant_type=" + (type == 1 ? "authorization_code" : "refresh_token")))) { //I really don't but I really do
                        postMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{ClientIdentity}:{ClienttSecret}")));
                        postMessage.Headers.Add("Content-Type", "application/json");
                        postMessage.Headers.Add("X-JOYSTICK-STATE", State);
                        //wait what was I doing? I need to switch types
                        var aaaaaaa = await hc.SendAsync(postMessage);


                        if (aaaaaaa.IsSuccessStatusCode) {
                            string contents = await aaaaaaa.Content.ReadAsStringAsync();
                            File.WriteAllText("dump.txt", contents);
                        } else
                            events.Print($"[HTTPClient]: There was an error processing request {aaaaaaa.StatusCode}", 3);
                        //need that token manager now, got any of those tokens for managing?
                    }
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
                    State = Generatestate();

                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", $"{Program.ACCESS_TOKEN}");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Add("X-JOYSTICK-STATE", State);
                    FormUrlEncodedContent postOptions;
                    string secretshitstoredsomewhereintheuniverse = "EEEEEEEEEEEEEEEHhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhhh"; //placeholder

                    if (!refreshRequestREEEEEEEE) {
                        postOptions = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "authorization_code"),
                            new KeyValuePair<string, string>("code", OAuthCode), //TODO This doesn't make a god damn bit of sense it isn't init
                            new KeyValuePair<string, string>("redirectUri", "")//RedirectURI)
                        });
                    } else {
                        //request frerefshres I DONT STROKE
                        postOptions = new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("grant_type", "refresh_token"),
                            new KeyValuePair<string, string>("refresh_token", secretshitstoredsomewhereintheuniverse),
                            new KeyValuePair<string, string>("redirectUri", "")//redirectURI)
                        });
                    }

                    //REVIEW README for this.
                    Uri HostEndPoint = new Uri( refreshRequestREEEEEEEE == true ? $"{Program.HOST}/api/oauth/token" : $"{Program.HOST}/api/oauth/authorize" );

                    var tokenRequest = await client.PostAsync(HostEndPoint, postOptions, cancellation);

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
