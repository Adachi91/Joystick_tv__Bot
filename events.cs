using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joystick_tv__Bot
{
    //Thanks to veccasalt for entertaining me while I wrote up this monsterousity of a class, also grabbed the WSS event types while viewing.
    class events
    {
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
            ApplicationChannel,
            SystemEventChannel,
            EventLogChannel,
            ChatChannel,
            WhisperChatChannel
        };

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
                case "subscribe":
                    //var sub = new subscription();
                    List<subscription> subbing = new List<subscription>();

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
    }
}
