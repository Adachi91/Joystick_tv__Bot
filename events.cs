using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client; //fucking samples don't match current version?
using IdentityModel.Internal;
using Microsoft.AspNetCore.Http.Features;

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

        private static string theChannel = token.channel;
        private static string _user_id;
        private static string _user_uuid;
        private static string _stream_id;
        private static string _userToken;

        public Dictionary<string, bool> Subscriptions = new Dictionary<string, bool>();

        public static async void testNow()
        {
            var client = new HttpClient();

            var response = await client.RequestTokenAsync(new TokenRequest
            {
                Address = "https://authorization-server.com/authorize",
                GrantType = "custom",

                ClientId = "client",
                ClientSecret = "secret",

                Parameters =
                {
                    { "custom_parameter", "custom value"},
                    { "scope", "api1" }
                }
             });
        }

        public class OAuthClient //makes sense to me because it's the client sheeeeeeeeeeeeesh
        {
            private static string Authority { get; set; }
            private static string redirectURI { get; set; }
            private static string scope { get; set; }
            private static string clientIdentity { get; set; }
            private static string clienttSecret { get; set; }
            private static string state { get; set; }
            //you barely have to write shit identitymodel has built in classes to format most things you were going to create so eh ya just read the docs.

            private static string playgroundnounce = "ehwNi41AnX1M8ljO";

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
             * why did I stop typing to text someone now I forgot where I was.
             * 
             * you have some work to do to figure out how to use this framework to spinup temporary webserver and make the call but should be easy, just need to read
             * the docs, it's very well documented.
             */

            //I'm going to work on this later I need to do a bit of the old reading.
            OAuthClient(string _authority, string _redirectURI, string _clientidentity, string _clientseret, string _scope = null)
            {
                Authority = _authority;
                redirectURI = _redirectURI;
                clientIdentity = _clientidentity;
                clienttSecret = _clientseret;
                scope = _scope ?? "ALLURBASES";
            }

            private static async Task LogIn()
            {
                browser idksomefuckedupshitalsodidyouknowthatunicornsarenotrealstoplookingforthemtheyrunawayeverytimeandleaveyoufeelingdeadinsideyo = new browser();
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

            switch (command)
            {
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
                    msg.Add(new { command = "message", identifier = new { channel = "ChatChannel", stream_id = _stream_id, user_id = _user_uuid  }, data = new { text =  message, token = _userToken, action = "send_message" } });
                    break;
                case "disconnect": //Call unsunscribe first  ? pls thx luv u ♥
                    msg.Add(new { command = "unsubscribe", identifier = "{\"channel\":\"ApplicationChannel\"}" });
                    msg.Add(new { command = "unsubscribe", identifier = "{\"channel\":\"SystemEventChannel\",\"user_id\":\"" + _user_id + "\"}" });
                    break;
                default:
                    throw new Exception("An invalid call to MessageConstructor was passed.");
                    break;
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
