using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShimamuraBot.Modules
{
    internal class OverUnder
    {
        private bool _open = false;
        private string _prizeredeem = "";
        private Dictionary<string, int> _contestants = new Dictionary<string, int>();

        public OverUnder(string prize) {
            _prizeredeem = prize;
            _open = true;
            _ = SendMessage("send_message", new string[] { $"A new Over/Under game has started! place your bets now for free redeem of {prize}. To enter type .over or .under" });
        }

        public void addContestant(string contestant, int bet) {
            if(_open && !_contestants.ContainsKey(contestant))
                _contestants.Add(contestant, bet);
        }

        public void closeEntry() {
            _open = false;
            _ = SendMessage("send_message", new string[] { "Over/Under entries have been closed!" });
        }

        public async Task getResults(int result) {
            foreach(var winner in _contestants) {
                if (winner.Value == result) {
                    await Modules.GamesHandler.UpdateRewards(winner.Key, _prizeredeem, 1);
                }
            }
            _ = SendMessage("send_message", new string[] { $"Over/Under has ended! Congratulations to the winners!" });
        }
    }
}
