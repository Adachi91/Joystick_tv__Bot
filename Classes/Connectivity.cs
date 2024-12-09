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
        /// <summary>
        ///  Attempt to ping an outside resource (1.1.1.1) to see if there is connectivity.
        /// </summary>
        /// <returns>Boolean - True=Connected || False=No_Connection</returns>
        public static bool Ping() {
            using (Ping ping = new Ping()) {
                PingOptions pingOptions = new PingOptions();
                pingOptions.Ttl = 32;
                pingOptions.DontFragment = true;

                byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                byte[] cloudFlufferIP = { 0x31, 0x2E, 0x31, 0x2E, 0x31, 0x2E, 0x31 }; /// I have no idea why I did this. But I'm leaving it.

                PingReply reply = ping.Send("1.1.1.1", 999, buffer, pingOptions);

                if (reply.Status == IPStatus.Success) {
                    return true;
                } else if(reply.Status == IPStatus.TimedOut || reply.Status == IPStatus.TimeExceeded || reply.Status == IPStatus.DestinationNetworkUnreachable) {
                    return false;
                }

                new BotException("Connectivity-Pinger", "Ping fell through and you are here no./, Welcome.");
                //fall through
                return false;
            }
        }

        /// <summary>
        ///  Attempt to ping an outside resource (1.1.1.1) to see if there is connectivity.
        /// </summary>
        /// <remarks>Purely for sementics, to run checks to see if it's false see <see cref="Ping"/> for full Method.</remarks>
        /// <returns>Boolean - True:Connected, False:NOCON</returns>
        public static bool NoPing() => !Ping();

        public static bool InspectHost() {
            // Not implemented.
            return false;
        }
    }
}
