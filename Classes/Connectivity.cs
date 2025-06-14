using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes
{
    internal class Connectivity
    {
        private static string name = "Ping";

        /// <summary>
        ///  Attempt to ping an outside resource (1.1.1.1) to see if there is connectivity.
        /// </summary>
        /// <returns><see cref="bool"/> True:Connected || False:NOCON</returns>
        public async static Task<bool> Ping(string host = "1.1.1.1") {
            int maxTries = 5;

            while (true) {
                bool success = _Ping(host);
                if(success) {
                    return true;
                } else {
                    if (maxTries-- == 0) return false;

                    await Task.Delay(111);
                }
            }
        }

        
        private static bool _Ping(string host = "1.1.1.1") { // this needs to be more roboust. it can fail fast
            using (Ping ping = new Ping()) {
                PingOptions pingOptions = new PingOptions();
                pingOptions.Ttl = 32;
                pingOptions.DontFragment = true;

                byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                //byte[] cloudFlufferIP = { 0x31, 0x2E, 0x31, 0x2E, 0x31, 0x2E, 0x31 }; /// I have no idea why I did this. But I'm leaving it.

                PingReply reply = ping.Send(host, 300, buffer, pingOptions);

                if (reply.Status == IPStatus.Success) {
                    return true;
                } else if(reply.Status == IPStatus.TimedOut || reply.Status == IPStatus.TimeExceeded || reply.Status == IPStatus.DestinationNetworkUnreachable || reply.Status == IPStatus.Unknown) {
                    new BotException(name, $"Ping status: {reply.Status}");
                    return false;
                }

                new BotException(name, "Ping fell through and you are here no./, Welcome.");
                //fall through
                return false;
            }
        }

        /// <summary>
        ///  Attempt to ping an outside resource (1.1.1.1) to see if there is connectivity.
        /// </summary>
        /// <remarks>Purely for sementics, to run checks to see if it's false see <see cref="Ping"/> for full Method.</remarks>
        /// <returns><see cref="bool"/> True:NOCON, False:Connected</returns>
        public static bool NoPing() => !Ping().Result;

        public static bool InspectHost() { /// To see if the HOST is down (e.g. Twitch, Joyszitck).
            // Not implemented.
            // only problem is that the hostname that is known by RTMP digest might and probably will not be the same as the endpoint.
            return false;
        }
    }
}
