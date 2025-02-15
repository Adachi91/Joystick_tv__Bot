using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes.Interface {
    internal interface IWebSocketService : IDisposable {
        #region Generics
        Task<bool> Connect();

        [Obsolete("Handle as much of the state internally.")]
        Task Reconnect(); // purge this, let it internally handle it otherwise you'll fuck something up.
        [Obsolete("I control the cancellationtokensource, I can trip it internally (WSClient)")]
        Task<bool> Disconnect(bool connection_fault); // One problem with letting it control based on CTS is that the socket will slam shut the second cancelled.

        [DoesNotReturn]
        Task Receive(string msg);
        #endregion

        void RegisterCloseAsync(Func<Task<bool>> CloseDelegate);
        void RegisterConnectAsync(Func<Task<bool>> ConnectDelegate);
        void RegisterSend(Func<string, Task<bool>> SendDelegate); // After a reference to the socket is made, everything else can be handled in the service's class.

        string name { get; }
        string Host { get; }
        string Endpoint { get; }
        string Internal_Host { get; } // Used only for echoing what the fuck exploded.

        // foreach(idk in services) if(enabled) service = new();

        // (^o^ )\ ふぁいと！ - 2019/07/07 - 23:00 ??? idk co-pilot wrote it.
        //void Reply(params string[] moo);
        //void Whisper(params string[] moo);
        //Task<bool> Send(params string[] moo);

        //void Kick(params string[] moo);

        /*
         * Main -> New Joystick() -> New Joystick.WebSocket() -v
         *  new WebSocketClient<Joystick.Websocket> ->
         *      Access: Send() method,
         *      Receives: OnMessage() method,
         *      
         *      
         *      Possibilities:
         *          Expose socket
         *          Invoke ConnectAsync() && CloseAsync(), registering them as well as delegates.
         *          
         *      Can't nots:
         *          Make it static (No new spawning so only single service)
         *          
         * 
         * 
         */
    }
}
