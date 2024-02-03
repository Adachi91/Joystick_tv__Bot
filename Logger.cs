using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ShimamuraBot
{
    internal class Logger
    {
        public static void appendFile(Dictionary<DateTime, Tuple<string, string>> log) {
            if (!File.Exists(HISTORY_PATH)) File.Create(HISTORY_PATH);

            foreach (var item in log) {
                File.AppendText($"{Loggertime(item.Key)}Channel: {item.Value.Item1} - Message: {item.Value.Item2}");
            }
        }

        public static void appendFile(string log) {
            if(!File.Exists(HISTORY_PATH)) File.Create(HISTORY_PATH);

            File.AppendText($"{LoggerTime()}{log}");
        }


        private static string Loggertime(DateTime time) {
            DateTime dateTime = time;
            string formattedDate = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");

            return $"[{formattedDate.Substring(2, 8)}][{formattedDate.Substring(11)}] - ";
        }

        private static string LoggerTime () {
            DateTime dateTime = DateTime.Now;
            string formattedDate = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");

            return $"[{formattedDate.Substring(2, 8)}][{formattedDate.Substring(11)}] - ";
        }
    }
}
