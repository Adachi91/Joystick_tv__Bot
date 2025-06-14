using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Json;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace ShimamuraBot.Modules
{
    internal class GamesHandler {
        private static string name = "PointsHandler";
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        #region Prize_Struct
        private class UserProperties {
            public required int Currency { get; set; }
            public Dictionary<string, bool>? Redeems { get; set; }
        }
        #endregion
        /* Current Struct:
         * [
         *      {
         *          "Username": "Paul",
         *          "Prizes": {
         *              "Verb": {
         *                  "Amount": int
         *              }
         *          }
         *      }
         * ]
         * 
         * Updated:
         * {
         *      "Paul": {
         *          "Currency": int,
         *          "Redeems": {
         *              "Name": Bool
         *          }
         *      }
         * }
         * 
         */

        private enum Redeemables : int {
            duck = 10,
            dump = 20,
            trip = 30,
            rage = 40,
            eyes = 50,
            breasts = 60
        }

        /// <summary>
        ///  idk some stupid shit. This is that stupid shit I'm talkin about when I do stupid shit.
        /// </summary>
        public enum PointsOptions {
            None,
            Get,
            Announce,
            Redeem,
            Update
        }

        /// <summary>
        ///  Sample redeemer to interface with vNyan while I create a more robust one that will interface with 3rd-party apps / native redeems
        /// </summary>
        /// <remarks>All fields are mandatory except for Username even though it's a required param.<br/> it's only used for when it is a non-tip redeem<br/>e.g. they won a free redeem from one of the games_modules or were granted free redeem.</remarks>
        /// <param name="username">String - username (Only works with non-tip redeems)</param>
        /// <param name="rTxt">String - Redeemable</param>
        /// <param name="tipped">Bool - True if they tipped otherwise false (Bypass eligible check)</param>
        /// <param name="time">int - How long in SECONDS to perform action if applicable</param>
        /// <param name="toggle">Bool - Is Toggable? Need to send end message on callback</param>
        public static async Task Redeemer(string username, string rTxt, bool tipped = false, int time = 0, bool toggle = false) {
            bool _eligible = true;//!IMPORTANT remove new instansiated vNayan classes and use only the 1 open socket.
            VNyan nyan = new VNyan(); // ref -- TODO: REMOVE THIS - USE Using(vNyan nyan = new ())

            if (!tipped)
                _eligible = await ((dynamic)isEligible(username, rTxt, 100, PointsOptions.Redeem)).b;

            if(time > 0) {
                await Task.Run(async () => {
                    nyan.Redeem(rTxt);
                    await Task.Delay(time * 1_000);

                    if (toggle) {
                        if (rTxt.ToLower().Contains("tits")) { rTxt = "notits"; }
                        nyan.Redeem(rTxt); } //toggle again to turn off then done.
                });
            } else {
                if (_eligible)
                    nyan.Redeem(rTxt);
            }
            //nyan = null; //gcc GOOOOOOOOOOOOOOOOO idk nullify it so gc will f!@# it like a lost&found (used) pocket toy
            Print(name, $"internal:Redeeming: {rTxt}", PrintSeverity.Debug);
        }

        /// <summary>
        ///  Gets the current list of rewards the user has and whispers it back to them.
        /// </summary>
        /// <param name="username">Required - username</param>
        /// <param name="prize">Do not use</param>
        /// <param name="magicNumber">Do not use</param>
        /// <returns>Dict<>?</returns>
        public static async Task<object> GetRewards(string username, string prize = "", int amount = 0, PointsOptions options = PointsOptions.Announce) => await CheckRewards(username, prize, amount, options);


        //check if user is eligible for reward, and if they are the logic in CheckRewards will return true and reduce the count or remove it
        /// <summary>
        ///  Checks if a user is eligible for redeem, only call this on redeem attempt as it will automatically reduce the reward on check
        /// </summary>
        /// <param name="username">User</param>
        /// <param name="reward">Redeem Name</param>
        /// <returns>Bool - True if eligible, Otherwise False</returns>
        private static async Task<object> isEligible(string username, string reward, int amount, PointsOptions options) => await CheckRewards(username, reward, amount, options);

        
        /// <summary>
        ///  Update/Set redeem count for user
        /// </summary>
        /// <param name="username">User</param>
        /// <param name="reward">Redeem Name</param>
        /// <param name="amount">Amount of redeems to award</param>
        /// <returns></returns>
        public static async Task UpdateRewards(string username, string reward, int amount, PointsOptions options, bool enable) => await CheckRewards(username, reward, amount, options, enable);


        /// <summary>
        ///  Main logic area, it's a mess checks elgibility, updates/creates/echos
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="rewardName">Redeem Name</param>
        /// <param name="amount">Amount to award, pseudoMAGIC NUMBERS: 0, 69 do not use these</param>
        /// <returns>Bool - (Optional) Eligibility check</returns>
        private static async Task<object> CheckRewards(string username, string rewardName, int amount, PointsOptions options = PointsOptions.None, bool enable = false) {
            var _eligible = false;
            var _deadlockPrevention = false;
            bool _changed = false;
            bool _return = false;

            /*
             * Operations:
             * Read 'file'.json -> parse to strongly typed class (Using the struct above)
             * Check if user exists;
             * Check if user is eligible && reduce;
             * Pull points -> WebSocket.SendWhisperAsync(pts);
             */

            dynamic MOO = new {
                b = false,
                m = ""
            };

            await _semaphore.WaitAsync();
            try {
                if (!File.Exists("rewards.json"))
                    await File.WriteAllTextAsync("rewards.json", "[]"); // [ ] ???????????????????????????????????????????????????????????????

                string rewardFileLines = await File.ReadAllTextAsync("rewards.json");
                Dictionary<string, UserProperties> users = JsonSerializer.Deserialize<Dictionary<string, UserProperties>>(rewardFileLines) ?? new();

                var pulledUser = users.FirstOrDefault(w => w.Key == username);

                if (pulledUser.Value == null) { users.Add(username, new UserProperties { Currency = 0, Redeems = { } }); _changed = true; }

                pulledUser = users.FirstOrDefault(w => w.Key == username);


                switch (options) {
                    case PointsOptions.Update:
                        pulledUser.Value.Currency += amount;
                        if(!string.IsNullOrEmpty(rewardName)) pulledUser.Value.Redeems!.Add(rewardName, enable);
                        _changed = true;
                        break;
                    case PointsOptions.Get:

                        break;
                    case PointsOptions.Announce:
                        string message = $"Points: {pulledUser.Value.Currency}";
                        foreach (var f in pulledUser.Value.Redeems!) // Taking bets on if this will throw AAAAAAAAAAAAAAAAAAAAYOOOOOOOOOOOOOO
                            if(f.Value == true)
                                message += $"\r\n{f.Key}: Free USe";
                        MOO.b = true;
                        MOO.m = message;
                        _return = true;
                        break;
                    case PointsOptions.Redeem:
                        if(pulledUser.Value.Currency >= amount) {
                            // don't worry about it. This stays.
                            pulledUser.Value.Currency = (int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(int)(pulledUser.Value.Currency - amount);
                            _changed = true;
                            MOO.b = true;
                            MOO.m = "";
                            _changed = true;
                        } else {
                            // FUCKING NOTHING
                            MOO.m = "";
                        }
                        break;
                    default:
                        throw new BotException(name, "Invalid operation.");
                }


                if (_changed) {
                    if (users.ContainsKey(pulledUser.Key))
                        users.Remove(pulledUser.Key);
                    users.Add(pulledUser.Key, pulledUser.Value);

                    var updatedRewards = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync("rewards.json", updatedRewards);
                }















                /*List<UserProperties> rewardList = JsonSerializer.Deserialize<List<UserProperties>>(rewardFileLines) ?? new List<UserProperties>();

                var winner = rewardList.FirstOrDefault(w => w.Username == username);
                if (winner == null && amount == 0) //usernot found, and it's not an eligibility check
                    _deadlockPrevention = true;
                else if (winner == null && amount > 0) { //user not found, and amount is given create user
                    winner = new UserProperties { Username = username };
                    rewardList.Add(winner);
                }


                if (!_deadlockPrevention) {
                    if (winner.Prizes.ContainsKey(rewardName)) {
                        if (winner.Prizes[rewardName].Amount >= 1 && amount == 0) { //eligibility check and reducer (only called on redeem)
                            _eligible = true;
                            winner.Prizes[rewardName].Amount--;
                        } else
                            winner.Prizes[rewardName].Amount += amount; //can be a negative int to reduce amount

                        if (winner.Prizes[rewardName].Amount <= 0)
                            winner.Prizes.Remove(rewardName);
                    } else //prizeKey wasn't found so adding it with amount
                        winner.Prizes.Add(rewardName, new Prize { Amount = amount });

                    var updatedRewards = JsonSerializer.Serialize(rewardList, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync("rewards.json", updatedRewards);
                }*/
            } catch (Exception ex) {
                new BotException("RewardHandler", "Exception thrown: ", ex);//never throw inside a catch only try, otherwise new. DEADLOCK
            } finally {
                _semaphore.Release();
            }
            return MOO;
            //return _eligible;
        }
    }
}
