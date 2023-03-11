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
        public bool streamSuccess;
        public bool appSuccess;

        public class subscription
        {
            public string command { get; set; }
            public identifier identifier { get; set; }
        }

        public class unsubscribe
        {
            public string command { get; set; }
            public identifier identifier { get; set; }
        }

        public class identifier
        {
            public string channel { get; set; }
            public string user_id { get; set; }
            public string stream_id { get; set; }
        }

        enum Channels {
            ApplicationChannel, //only required on first connection or reconnection
            SystemEventChannel, //as above.
            EventLogChannel,//Channel_Specific
            ChatChannel,//Channel_Specific
            WhisperChatChannel//Channel_Specific
        };

        public Object MessageConstructor()
        {

        }

        /// <summary>
        /// Constructs Subscription and Unsubscribing events for a channel.
        /// </summary>
        /// <param name="command">Type of channel to construct</param>
        /// <param name="stream_id">The target stream</param>
        /// <param name="user_id">Bot name UNLESS ChatChannel is subscription then UUID is required</param>
        /// <param name="user_UUID">Bot UUID</param>
        public events(string command, string stream_id, string user_id, string user_UUID)
        {
            switch (command) {
                case "connect":
                    List<Object> Connection = new List<object>
                    {
                        new { command = "Subscribe", identifier = new { channel = "ApplicationChannel" } },
                        new { command = "Subscribe", identifier = new { channel = "SystemEventChannel", user_id = user_id } },
                    };

                    //return Connection;
                    break;
                case "subscribe":
                    //var sub = new subscription();
                    List<subscription> subbing = new List<subscription>();
                    List<Object> Subscription = new List<object> {
                        new { command = "Subscribe", identifier = new { channel = "EventLogChannel", stream_id = stream_id } },
                        new { command = "Subscribe", identifier = new { channel = "ChatChannel", stream_id = stream_id, user_id = user_UUID } },
                        new { command = "Subscribe", identifier = new { channel = "WhisperChatChannel", user_id = user_id, stream_id = stream_id } },
                    };
                    //sub.identifier.channel = ;
                    //sub.identifier.stream_id = "";
                    foreach(string channel in Enum.GetValues(typeof(Channels)))
                    {
                        switch(channel)
                        {
                            case "ApplicationChannel": //only requires sending type.
                                subscription ApplicationChannel = new subscription();
                                ApplicationChannel.command = "subscribe";
                                ApplicationChannel.identifier.channel = channel;
                                subbing.Add(ApplicationChannel);
                                break;
                            case "SystemEventChannel":
                                subscription SystemEventChannel = new subscription();
                                SystemEventChannel.command = "subscribe";
                                SystemEventChannel.identifier.user_id = user_id;
                                SystemEventChannel.identifier.channel = channel;
                                subbing.Add(SystemEventChannel);
                                break;
                            case "EventLogChannel":
                                break;
                            case "ChatChannel": //requires UUID
                                break;
                            case "WhisperChatChannel":
                                break;
                        }
                    }
                    break;
                case "unsubscribe":
                    var unsub = new unsubscribe();
                    break;
                default:
                    break;
        }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release any managed resources here
                }

                // Release any unmanaged resources here
                _disposed = true;
            }
        }
    }
}
