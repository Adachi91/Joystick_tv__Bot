﻿using System;
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
    internal class GamesHandler
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        #region Prize_Struct
        private class Prize {
            public int Amount { get; set; }
        }

        private class Winner {
            public string Username { get; set; }
            public Dictionary<string, Prize> Prizes { get; set; }

            public Winner()
            {
                Prizes = new Dictionary<string, Prize>();
            }
        }
        #endregion


        /// <summary>
        ///  Gets the current list of rewards the user has and whispers it back to them.
        /// </summary>
        /// <param name="username">Required - username</param>
        /// <param name="prize">Do not use</param>
        /// <param name="magicNumber">Do not use</param>
        /// <returns>Dict<>?</returns>
        public static async Task GetRewards(string username, string prize = "", int magicNumber = 69) =>
            await CheckRewards(username, prize, magicNumber); //This is not implemented yet!


        //check if user is eligible for reward, and if they are the logic in CheckRewards will return true and reduce the count or remove it
        /// <summary>
        ///  Checks if a user is eligible for redeem, only call this on redeem attempt as it will automatically reduce the reward on check
        /// </summary>
        /// <param name="username">User</param>
        /// <param name="reward">Redeem Name</param>
        /// <returns>Bool - True if eligible, Otherwise False</returns>
        public static async Task<bool> isEligible(string username, string reward) =>
            await CheckRewards(username, reward);

        
        /// <summary>
        ///  Update/Set redeem count for user
        /// </summary>
        /// <param name="username">User</param>
        /// <param name="reward">Redeem Name</param>
        /// <param name="amount">Amount of redeems to award</param>
        /// <returns></returns>
        public static async Task UpdateRewards(string username, string reward, int amount) =>
            await CheckRewards(username, reward, amount);


        /// <summary>
        ///  Main logic area, it's a mess checks elgibility, updates/creates/echos
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="rewardName">Redeem Name</param>
        /// <param name="amount">Amount to award, pseudoMAGIC NUMBERS: 0, 69 do not use these</param>
        /// <returns>Bool - (Optional) Eligibility check</returns>
        private static async Task<bool> CheckRewards(string username, string rewardName, int amount = 0) {
            //var prizefilelines = await File.ReadAllLinesAsync("winners.json");
            // var currentPrizeList = JsonSerializer.Deserialize<Winner>(prizefilelines); //deserialize all current entries, for updating
            //Protip - never trust a robot topkek. it keeps trying to Return false; and potentional cause a deadlock. when asked if the code is optimized
            var _eligible = false;
            var _deadlockPrevention = false;

            await _semaphore.WaitAsync();
            try {
                if (!File.Exists("rewards.json"))
                    await File.WriteAllTextAsync("rewards.json", "[]");

                var rewardFileLines = await File.ReadAllTextAsync("rewards.json");
                var rewardList = JsonSerializer.Deserialize<List<Winner>>(rewardFileLines) ?? new List<Winner>();

                var winner = rewardList.FirstOrDefault(w => w.Username == username);
                if (winner == null && amount == 0) //usernot found, and it's not an eligibility check
                    _deadlockPrevention = true;
                else if (winner == null && amount > 0) { //user not found, and amount is given create user
                    winner = new Winner { Username = username };
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
                }
            } catch (Exception ex) {
                new BotException("RewardHandler", "Exception thrown: ", ex);//never throw inside a catch only try, otherwise new. DEADLOCK
            } finally {
                _semaphore.Release();
            }
            
            return _eligible;
        }
    }
}
