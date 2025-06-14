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
        private int _max = 100;
        private string _prizeredeem = "";
        private Dictionary<string, Tuple<int, int>> _contestants = new();

        public OverUnder(string prize, int maxBet) {
            _prizeredeem = prize;
            _max = maxBet;
            _open = true;
            _ = SendWebSocketMsg("send_message", $"A new Over/Under game has started! place your bets now, max bet: {_max}. To enter type \".over bet\" or \".under bet\"");
        }

        public void addContestant(string contestant, int direction, int bet) {
            if(_open && !_contestants.ContainsKey(contestant))
                _contestants.Add(contestant, new Tuple<int, int>(direction, bet));
        }

        public void closeEntry() {
            _open = false;
            _ = SendWebSocketMsg("send_message", "Over/Under entries have been closed!");
        }

        public async Task getResults(int result) {
            foreach(var winner in _contestants) {
                if (winner.Value.Item1 == result) {
                    await Modules.GamesHandler.UpdateRewards(winner.Key, "", 1, PointsOptions.Update, false);
                }
            }
            _ = SendWebSocketMsg("send_message", $"Over/Under has ended! Congratulations to the winners!");
        }
    }
}
