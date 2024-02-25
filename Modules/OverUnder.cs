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
            //send_message("A new Over/Under has been started! place your bets for a free {prize} redeem")
        }

        public void addContestant(string contestant, int bet) {
            if(_open && !_contestants.ContainsKey(contestant))
                _contestants.Add(contestant, bet);
        }

        public void closeEntry() {
            _open = false;
        }

        public async Task getResults(int result) {
            foreach(var winner in _contestants) {
                if (winner.Value == result) {
                    _ = Modules.GamesHandler.UpdateRewards(winner.Key, _prizeredeem, 1);
                }
            }
            //send_message("Over/Under has ended! Congratulations to the winners!") //do not announce all winners could be too large
        }
    }
}
