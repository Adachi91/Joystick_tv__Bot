using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;//remove before compiling builds only used for debugger

namespace ShimamuraBot
{
    internal class Logger
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        ///  Write to log file
        /// </summary>
        /// <param name="type">The event Type from WSSClient</param>
        /// <param name="args">string[DateTime, string, string]</param>
        /// <returns></returns>
        public static async Task WriteToFileShrug(string type, params string[] args) { //passed - Optimize
            if (args == null || args.Length == 0)
                throw new ArgumentNullException("Arguments are empty. They are required.");


            if(!LOGGING_ENABLED && !Debugger.IsAttached) { return; }

            await _semaphore.WaitAsync();
            try {
                StringBuilder sb = new StringBuilder();
                if (DateTime.TryParse(args[0], out DateTime date))
                    sb.Append($"{LoggerTime(date)}[{type}] -");
                else
                    throw new ArgumentException("The first argument was not a DateTime value.");

                for (int i = 1; i < args.Length; i++)
                    sb.Append($" {args[i]}");

                await File.AppendAllTextAsync(HISTORY_PATH, sb.ToString() + Environment.NewLine);
            } catch (FormatException) {
                Print($"[Logger]: Tried to log an entry without a TimeDate value", 3);
            } catch (Exception ex) {
                Print($"[Logger]: {ex}", 3);
            } finally { _semaphore.Release(); }
        }


        private static string LoggerTime(DateTime time) {
            DateTime dateTime = time;
            string formattedDate = dateTime.ToString("yyyy-MM-ddTHH:mm:ss");

            return $"[{formattedDate.Substring(2, 8)}][{formattedDate.Substring(11)}] - ";
        }
    }
}
