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
using System.Net.Http.Json;
using System.Text.Json;
using System.Reflection.PortableExecutable;
using Microsoft.VisualBasic;
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
            Console.SetCursorPosition(0, Console.CursorTop);
            string[] ctx = formatPrint(text); //index 0 is Tag from which class / service. index 1 is the message.

            switch (level) {
                case 0:
                    #if DEBUG
                        Console.ForegroundColor = debug; Console.Write($" [Debug]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n"); 
                    #endif
                    break;
                case 1: ctx = formatPrint(text); Console.WriteLine($" [System]{ctx[0]}: {ctx[1]}");
                    break;
                case 2: Console.ForegroundColor = warn; Console.Write($" [WARN]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n");
                    break;
                case 3: Console.ForegroundColor = error; Console.Write($" [ERROR]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}\r\n");
                    break;
                case 4:
                    Random rand = new Random();
                    Console.WriteLine($"If you're seeing this then somehow, somewhere in this vast universe someone invoked the wrath of Hel▲6#╒e¢◄e↕Y8AéP╚67/Y1R\\6xx9/5Ωφb198 . . . . . {(Death)Enum.GetValues(typeof(Death)).GetValue(rand.Next(Enum.GetValues(typeof (Death)).Length))}");
                    break;
                default: Console.WriteLine($"I don't even want to know.");
                    break;
            }
            Console.Write(">");
        }
        #endregion


        /// <summary>
        /// Get the current UTC Unix Timestamp
        /// </summary>
        /// <returns>(long) Timestamp</returns>
        public static long GetUnixTimestamp()
        {
            long timestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            return timestamp;
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
