using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;

namespace ShimamuraBot
{
    internal class Logger
    {
        private static string name = "Logger";
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static StringBuilder sb = new();

        /// <summary>
        ///  Write to log file
        /// </summary>
        /// <param name="component">The component or event you are logging from</param>
        /// <param name="args">string[string*] list of grievences you would like to talk about</param>
        public static async Task LogAsync(string component, params string[] args) { //passed - Optimize
            if(!LOGGING_ENABLED && !Debugger.IsAttached) { return; }

            if (args == null || args.Length == 0)
                throw new BotException(name, $"Arguments are empty. They are required.");

            await _semaphore.WaitAsync();
            try {
                //string date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                sb.Append($"[{DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")}] [{component}] -");

                for (int i = 0; i < args.Length; i++)
                    sb.Append($" {args[i]}");

                //await File.AppendAllTextAsync(LOG_FILE_PATH, sb.ToString() + Environment.NewLine);
                using (StreamWriter sr = new(LOG_FILE_PATH, append: true))
                    await sr.WriteAsync($"{sb}{Environment.NewLine}");

            } catch (Exception ex) {
                new BotException(name, "Uncaught Exception.", ex);
            } finally { sb.Clear(); _semaphore.Release(); }
        }
    }
}
