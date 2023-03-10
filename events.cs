using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joystick_tv__Bot
{
    //Thanks to veccasalt for entertaining me while I wrote up this monsterousity of a class, also grabbed the WSS event types while viewing.
    class events : IDisposable
    {

        private bool _disposed = false;

        private string _user_id;
        private string _user_uuid;
        private string _stream_id;
        private string _userToken;
        public bool streamSuccess;
        public bool appSuccess;


        enum Channels {
            ApplicationChannel, //only required on first connection or reconnection
            SystemEventChannel, //as above.
            EventLogChannel,//Channel_Specific
            ChatChannel,//Channel_Specific
            WhisperChatChannel//Channel_Specific
        };

        /// <summary>
        /// Construct a Message to send to the Socket.
        /// </summary>
        /// <param name="command">Command type (connect, subscribe, unsubscribe, sendmessage, disconnect)</param>
        /// <param name="message">Only required for message type sendmessage</param>
        /// <returns></returns>
        public List<Object> MessageConstructor(string command, string message = "")
        {
            List<Object> msg = new List<object>();

            var test = ":\"{ \"channel\":\"ApplicationChannel\"}";
            var test2 = "\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"" + _user_id + "\"}";

            var test4 = "{\"command\":\"subscribe\",\"identifier\":\"{ \"channel\":\"ApplicationChannel\"}\"}";
            var test6 = "{\"command\":\"subscribe\",\"identifier\":\"{\"channel\":\"SystemEventChannel\",\"user_id\":\"shimamura\"}\"}";

            switch (command)
            {
                case "connect":
                    msg.Add(new { command = "subscribe", identifier = new { channel = "ApplicationChannel" } });
                    //msg.Add(new { command = "subscribe", identifier = test });
                    msg.Add(new { command = "subscribe", identifier = new { channel = "SystemEventChannel", user_id = _user_id } });
                    //msg.Add(new { command = "subscribe", identifier = test2 });
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
                    msg.Add(new { command = "unsubscribe", identifier = new { channel = "ApplicationChannel" } });
                    msg.Add(new { command = "unsubscribe", identifier = new { channel = "SystemEventChannel", user_id = _user_id } });
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

                _disposed = true;
            }
        }
    }
}
