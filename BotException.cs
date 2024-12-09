using System;
using System.Diagnostics.CodeAnalysis;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

namespace ShimamuraBot
{
    // new BotExceptions are GC'd so it's okay to call them as such.
    internal class BotException : Exception
    {
        /// <summary>
        ///  Send error message to the buffer
        /// </summary>
        /// <param name="sender">Oriigns</param>
        /// <param name="msg">Message</param>
        public BotException(string sender, string msg) : base(msg) {
            Print(sender, msg, PrintSeverity.Error);
        }

        /// <summary>
        ///  Send error message, and the inner exception through to the buffer.
        /// </summary>
        /// <param name="sender">Origins</param>
        /// <param name="msg">Message</param>
        /// <param name="inner">Exception exception</param>
        public BotException(string sender, string msg, Exception inner) : base(msg, inner) {
            Print(sender, $"{msg}. InnerException: {inner}", PrintSeverity.Error);
        }

        /*public void ThrowIfCancelled<t>(t value) where t : cancel {

        }*/

        [DoesNotReturn]
        private void ThrowIfCancelledException(CancellationToken cancellationToken) => throw new BotException("UNKNOWN! because", $"{nameof(cancellationToken)} exploded?");

        public void ThrowIfCancelled(CancellationToken cancellationToken) {
            if (cancellationToken.IsCancellationRequested) {
                ThrowIfCancelledException(cancellationToken);
            }
        }
    }
}
