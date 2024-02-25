using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace ShimamuraBot
{
    internal class BotException : Exception
    {
        /// <summary>
        ///  Send error message to the buffer
        /// </summary>
        /// <param name="sender">Oriigns</param>
        /// <param name="msg">Message</param>
        public BotException(string sender, string msg) : base(msg) {
            Print($"[{sender}]: {msg}", 3);
        }

        /// <summary>
        ///  Send error message, and the inner exception through to the buffer.
        /// </summary>
        /// <param name="sender">Origins</param>
        /// <param name="msg">Message</param>
        /// <param name="inner">Exception exception</param>
        public BotException(string sender, string msg, Exception inner) : base(msg, inner) {
            Print($"[{sender}]: {msg} :: {inner}", 3);
        }
    }
}
