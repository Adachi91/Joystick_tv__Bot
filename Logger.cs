using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Security.AccessControl;

namespace ShimamuraBot
{
    internal class Logger
    {
        private Dictionary<DateTime, Tuple<string, string>> messageQ = new Dictionary<DateTime, Tuple<string, string>>();


        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Write to log file
        /// </summary>
        /// <param name="type">The event Type from WSSClient</param>
        /// <param name="args">string[] - Args - The first MUST be DateTime</param>
        /// <returns></returns>
        public static async Task WriteToFileShrug(string type, params string[] args) { //passed - Optimize
            await _semaphore.WaitAsync();
            try {
                var date = Convert.ToDateTime(args[0]);

                StringBuilder sb = new StringBuilder();
                sb.Append($"{LoggerTime(date)}[{type}] -");

                for (int i = 1; i < args.Length; i++)
                    sb.Append($" {args[i]}");

                await File.AppendAllTextAsync(HISTORY_PATH, sb.ToString() + Environment.NewLine);
            } catch (Exception ex) {
                Print($"[Logger]: encountered an exception :: {ex}", 3);
            } finally { _semaphore.Release(); }
        }


        public static void appendFile(Dictionary<DateTime, Tuple<string, string>> log) {
            if (!File.Exists(HISTORY_PATH)) File.Create(HISTORY_PATH);



            foreach (var item in log) {
                File.AppendText($"{LoggerTime(item.Key)}Channel: {item.Value.Item1} - Message: {item.Value.Item2}");
            }
        }


        public static void log(string type, params string[] args)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"{LoggerTime(DateTime.Now)}[{type}] - ");

                for (int i = 0; i < args.Length; i++) {
                    //i == 0 ? sb.Append($"{args[i]}") : sb.Append($" {args[i]}");
                    //why do C# ternary hates me
                    if (i == 0) sb.Append($"{args[i]}"); else sb.Append($" {args[i]}");
                }

                var output = sb.ToString();

                using (StreamWriter sr = new StreamWriter(HISTORY_PATH, true)) {
                    sr.WriteLine(output);
                }

            } catch (Exception ex) {
                Print($"[Logger]: I/O Exception most likely occured :: {ex.ToString()}", 3);
            }
        }


        private static string LoggerTime(DateTime time) {
            DateTime dateTime = time;
            string formattedDate = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");

            return $"[{formattedDate.Substring(2, 8)}][{formattedDate.Substring(11)}] - ";
        }


        /*private static string LoggerTime() {
            DateTime dateTime = DateTime.Now;
            string formattedDate = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");

            return $"[{formattedDate.Substring(2, 8)}][{formattedDate.Substring(11)}] - ";
        }*/
    }
}
