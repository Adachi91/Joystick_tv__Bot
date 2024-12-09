using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes {
    internal class Twitch {
        private readonly string name = "Twitch";
        private const string _irc_endpoint = "irc.chat.twitch.tv";
        private TcpClient _client { get; set; }

        public Twitch() {
            _client = new TcpClient();

        }

        public async Task<bool> ConnectAsync() {
            var name = $"{this.name}:TcpClient";

            if (DEBUGGING_ENABLED) Print(name, $"Attempting to connect to {_irc_endpoint}:6697.", PrintSeverity.Debug);

            try {
                await _client.ConnectAsync(_irc_endpoint, 6697);
                return true;
            }
            catch (BotException) { /* recursive prevention */ }
            catch (SocketException sEx) { new BotException(name, $"A socket exception has occured while connecting to {_irc_endpoint}", sEx); }
            catch (Exception ex) { new BotException(name, $"Could not connect to {_irc_endpoint}.", ex); }

            return false;
        }

        public async Task<bool> CloseAsync() {
            try {
                _client.Close();
                while(_client.Connected) {
                    await Task.Delay(30);
                }
                Print(this.name, $"The connect to {_irc_endpoint} has closed successfulewlj", PrintSeverity.Debug);
                return true;
            }
            catch (BotException) { }
            catch (Exception ex) {
                new BotException(this.name, "Unable to manipulate client state.", ex);
            }
            return false;
        }

        private async Task<bool> StartListeningAsync() {



            return true;
        }


        //public class WebSocket {
            //wheatwat do they even use websockets topkek
        //}
    }
}
