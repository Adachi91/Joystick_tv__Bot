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

        /// <summary>
        ///  This will be the raw message received by the socket, so you need to handle any additional parsing in the respective Service class.
        /// </summary>
        /// <param name="msg">The raw string (JSON)</param>
        [DoesNotReturn]
        Task Receive(string msg);
        #endregion

        /// <summary>
        ///  Registers a delegate to close the socket async.
        /// </summary>
        /// <param name="CloseDelegate"></param>
        void RegisterCloseAsync(Func<Task<bool>> CloseDelegate);
        /// <summary>
        ///  Registers a delegate to ConnectAsync to the socket.
        /// </summary>
        /// <param name="ConnectDelegate"></param>
        void RegisterConnectAsync(Func<Task<bool>> ConnectDelegate);
        /// <summary>
        ///  Registers a delegate to send a message to the socket.
        /// </summary>
        /// <param name="SendDelegate"></param>
        void RegisterSend(Func<string, Task<bool>> SendDelegate); // After a reference to the socket is made, everything else can be handled in the service's class.

        /// <summary>
        ///  Component name, naturally this is 'Websocket' however I put a delimiter of '{Service}:Websocket' on creation, so that if multiple websockets are open
        ///  you can easily see which one needs debugging if problems arise.
        /// </summary>
        string name { get; }
        string Host { get; }
        /// <summary>
        ///  The full host endpoint URI
        /// </summary>
        /// <remarks>This can contain a webtoken, so it's best to never expose it to the buffer/logging. Use <see cref="Internal_Host"/> instead.</remarks>
        string Endpoint { get; }
        /// <summary>
        ///  The host being connected to since some services use GET params for keys, you don't want to be displaying/logging it.
        /// </summary>
        string Internal_Host { get; }
        Service Service { get; }

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
