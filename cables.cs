using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot
{
    //cables.cableBuilder('adachi91');
    //cables.ApplicationChannel.subscription
    class cables
    {

        public string channelName { get; set; }

        public class ApplicationChannel {
            public bool subscriptionSent { get; set; }
            public bool subscriptionConfirmed { get; set; }
        }

        public class EventChannel {
            public bool subscriptionSent { get; set; }
            public bool subscriptionConfirmed { get; set; }
            public string subscribedUser { get; set; }
        }

        #region classes for serialization
        // firstmsg myDeserializedClass = JsonConvert.DeserializeObject<firstmsg>(myJsonResponse);
        public class firstmsg
        {
            public string command { get; set; }
            public string identifier { get; set; }
        }

        // secondmsg myDeserializedClass = JsonConvert.DeserializeObject<secondmsg>(myJsonResponse);
        public class secondmsg
        {
            public string command { get; set; }
            public string identifier { get; set; }
        }
        #endregion

        public bool cableBuilder(string _channelName) {
            if (!string.IsNullOrEmpty(_channelName))
                channelName = _channelName;

            return buildChannels();
        }

        private bool buildChannels()
        {
            
            return false;
        }

    }
}
