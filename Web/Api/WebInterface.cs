using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShimamuraBot.Web.Api {
    internal class WebInterface : IDisposable {
        private string name = "WebInterface";
        private HttpListener? _http_socket;// = new(); // all hot dogs and hamburgers are a socket. Debate over.
        private string _local_addr { get; set; } = "http://localhost";
        private int _local_port { get; set; }
        private int _is_running = 0;

        private HttpListenerContext? _SSEContext;
        private bool _is_SSE_connected = false;

        private HttpListenerContext? _OBSSSEContext;
        private bool _is_OBSSSE_connected = false;

        private CancellationTokenSource _cts;

        private Router _router = new();

        public int _ohno = 0; //HWAT? ARE YOU OKAY CO-PILOT? BLINK TWICE IF YOU'RE IN DANGER.

        private TcpListener _socket;
        private ConcurrentDictionary<Route, StreamWriter> _clients = new();


        public WebInterface(string url, int port = 8080, CancellationTokenSource? cts = null) {
            if (cts == null) _cts = new CancellationTokenSource(); else _cts = cts;

            _local_port = port;
            //_local_addr = url;

            _socket = new TcpListener(IPAddress.Parse(url), port);
        }

        ~WebInterface() {
            Dispose();
        }

        #region ConnectionManagerV2
        public void Start(CancellationTokenSource? cts = null) {
            if(_isRunning) { Print(name, "WebUI is already running.", PrintSeverity.Debug); return; }
            Interlocked.Exchange(ref _is_running, 1);

            if (cts == null && _cts.IsCancellationRequested) { _cts.Dispose(); _cts = new(); } else if (cts != null && !cts.IsCancellationRequested) { _cts.Dispose(); _cts = cts; } else { Print(name, "HANDLE IT", PrintSeverity.Debug); }
            Print(name, $"Starting WebUI", PrintSeverity.Debug);
            _socket.Start();
            Task.Run(() => AcceptConnetionAsync());
            
        }


        public void Stop() {
            if (!_isRunning) { Print(name, "WebUI is not running.", PrintSeverity.Debug); return; }
            Interlocked.Exchange(ref _is_running, 0);

            Print(name, "Shutting down.", PrintSeverity.Debug);
            _cts.Cancel();
            _socket.Stop();
        }


        #region connection_building
        private async Task AcceptConnetionAsync() {
            while(!_cts.IsCancellationRequested) {
                var client = await _socket.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientsAsync(client);
            }
        }


        private async Task HandleClientsAsync(TcpClient client) {
            using (var networkStream = client.GetStream())
            using (var reader = new StreamReader(networkStream))
            using (var writer = new StreamWriter(networkStream) { AutoFlush = true }) {
                string? request = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(request)) return;

                Print(name, $"Received request: {request}", PrintSeverity.Debug); // this will tell me what it looks like, I'm assuming a full request header.
                Route? result;

                if (request.Contains("/sse/obs_events")) {
                    result = await _router.RouteAsync(Route.OBS, reader, writer);
                    if (result != null)
                        if (AddClient(Route.OBS, writer)) {
                            Print(name, $"Added {result} to the connection pool.", PrintSeverity.Debug);
                            try { // keep-alive ? idk.
                                while (!_cts.IsCancellationRequested) {
                                    //https://stackoverflow.com/questions/30415012/sse-server-sent-events-client-keep-sending-requests-like-polling
                                    await Task.Delay(1000);
                                    await SendSSEAsync(Route.OBS, new { type = "ping", time = DateTime.UtcNow }.Stringify());
                                }
                            } catch (IOException) {
                                Print(name, $"Client: {result} has disconnected.", PrintSeverity.Debug);
                            } catch (Exception ex) {
                                new BotException(name, $"Unhandled exception in {result} connection.", ex);
                            } finally {
                                RemoveClient(Route.OBS);
                            }
                        } else if (request.Contains("/sse")) {
                            result = await _router.RouteAsync(Route.SSE, reader, writer);

                            if (result != null)
                                if (AddClient(Route.SSE, writer))
                                    Print(name, $"Added {result} to the connection pool.", PrintSeverity.Debug);
                        } else {
                            await _router.RouteAsync(request, reader, writer); // this is going to take some serious logic to keep it from imploding.
                        }
                }
            }
        }

        /// <summary>Adds a route to the connection pool.</summary>
        /// <param name="route"><see cref="Route"/> to add.</param>
        private bool AddClient(Route route, StreamWriter writer) => _clients.TryAdd(route, writer);
        /// <summary>Removes a route from the connection pool.</summary>
        /// <param name="route"><see cref="Route"/> to close.</param>
        private bool RemoveClient(Route route) => _clients.TryRemove(route, out _);
        #endregion


        public void SendSSEAudioAsync(string src, int? limit = null, bool repeat = false) => _ = SendSSEAsync(Route.OBS, new { type = "audio", src = src, timelimit = limit, repeat = repeat }.Stringify());
        public void SendSSEImageAsync(string src, int limit = 6) => _ = SendSSEAsync(Route.OBS, new { type = "image", src = src, timelimit = limit }.Stringify());
        public void SendSSEVideoAsync(string src) => _ = SendSSEAsync(Route.OBS, new { type = "video", src = src }.Stringify());


        /// <summary>
        ///  Sends message to client's writer.
        /// </summary>
        /// <param name="route">The Client's ID</param>
        /// <param name="json">The message in JSON format.</param>
        /// <returns><see cref="bool"/> True:Failure, False:Success</returns>
        public async Task<bool> SendSSEAsync(Route route, string json) {
            if (json.Length < 2) { new BotException(name, $"Tried to send an empty message to {route}."); return true; } // maybe some kind of validation?

            if (_clients.TryGetValue(route, out var writer)) {
                var err = await _router.SendSSEAsync(writer, json);

                if (err) {
                    Print(name, $"{route} has failed a request, and is being removed from the connection pool.", PrintSeverity.Warn);
                    if (!RemoveClient(route))
                        throw new BotException(name, $"Was unable to remove {route} from the connection pool.");
                    return true;
                }
            } else
                return true;
            return false;
        }


        /// <summary>
        ///  This is used for a broadcast message (e.g. Ping)
        /// </summary>
        /// <param name="msg"><see cref="string"/> Message in JSON format.</param>
        public async Task SendBroadcastAsync(string msg) {
            bool err = false;
            foreach(var clientId in _clients.Keys) {
                err = await SendSSEAsync(clientId, msg);

                if(err) {
                    Print(name, $"Client: {clientId} failed to receive a broadcast message, and will be removed from pool of clients.", PrintSeverity.Debug);
                    RemoveClient(clientId);
                }
            }
        }
        #endregion


        private int GetAvailablePort() {
            int port;
            try {
                using (TcpListener listener = new TcpListener(IPAddress.Loopback, 0)) {
                    listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    listener.Start();
                    port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    listener.Stop();
                    return port;
                }
            } catch {
                throw new BotException($"{this.name}:GetAvailablePort", $"Unable to get an available port for the web interface.");
            }
        }

        public enum Route : int {
            API = 1, // Generic API for the bot.
            OBS = 2, // OBS specific SSE for OBS Studio.
            SSE = 3 // Generic SSE for bundled html.
        }

        //public void SendSSEAudioAsync(string src, int? limit = null, bool repeat = false) => _ = SendSSEAsync(_OBSSSEContext!, new { type = "audio", src = src, timelimit = limit, repeat = repeat }.Stringify(), Route.OBS);
        //public void SendSSEImageAsync(string src, int limit = 6) => _ = SendSSEAsync(_OBSSSEContext!, new { type = "image", src = src, timelimit = limit }.Stringify(), Route.OBS);
        //public void SendSSEVideoAsync(string src) => _ = SendSSEAsync(_OBSSSEContext!, new { type = "video", src = src }.Stringify(), Route.OBS);

        public async Task<bool> Ping(Route rootwat = Route.OBS) {
            string json = new { type = "ping", time = DateTime.UtcNow }.Stringify();

            try { //  you know sometimes I think you have a good idea co-pilot, but then I realize you just steal my code.
                switch (rootwat) {
                    case Route.OBS: if(OBSHealthy) return await SendSSEAsync(_OBSSSEContext!, json, Route.OBS); break;
                    case Route.SSE: if(SSEHealthy) return await SendSSEAsync(_SSEContext!, json, Route.SSE); break;
                    default: throw new BotException($"{this.name}:Ping", $"Invalid route provided.");
                }
            } catch (Exception ex) {
                new BotException($"{this.name}:Ping", $"Unable to send ping the SSE.", ex);
                return false;
            } //  you know sometimes I think you have a good idea co-pilot, but then I realize you just steal my code.
                // lol

            return true; // This is a bit of a fuckmind, but it's fine. says Co-Pilot. However This is to keep fall-through from triggering a warning.
        }


        private async Task<bool> SendSSEAsync(HttpListenerContext ctx, string json, Route route) {
            switch(route) {
                case Route.OBS: if(!OBSHealthy) { new BotException(name, $"OBS SSE was not connected."); return false; } break;
                case Route.SSE: if(!SSEHealthy) { new BotException(name, $"SSE was not connected."); return false; } break;
                case Route.API: break;
                default: throw new BotException(name, "Invalid routing.");
            }

            if (ctx == null) { new BotException(name, $"HttpListenerContext was missing"); return false; }

            return await _router.SendSSEAsync(ctx, json);
            // For Vicky D
        }

        private bool OBSHealthy => (_OBSSSEContext != null && _is_OBSSSE_connected);
        private bool SSEHealthy => (_SSEContext != null && _is_SSE_connected);

        private async Task StartSSEAsync(HttpListenerContext context, Route router) {
            context.Response.ContentType = "text/event-stream";
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            context.Response.Headers.Add("Cache-Control", "no-cache");
            context.Response.Headers.Add("Connection", "keep-alive");
            context.Response.StatusCode = 200;

            switch (router) { // Setup the proper context for each Server-Sent Event.
                case Route.SSE: _SSEContext = context; _is_SSE_connected = true; break;
                case Route.OBS: _OBSSSEContext = context; _is_OBSSSE_connected = true; break;
                default: new BotException($"{this.name}:StartSSEAsync", $"Invalid route provided for SSE."); break;
            }

            using StreamWriter writer = new StreamWriter(context.Response.OutputStream);

            dynamic msg = new { type = "connected", time = DateTime.UtcNow }.Stringify();
            bool b = await SendSSEAsync(context, msg, router);
            //await writer.WriteLineAsync("event: message");
            //await writer.WriteLineAsync($"data: {msg}");
            //await writer.WriteLineAsync();
            //await writer.FlushAsync();
            if (b) Print(name, $"Connected to {router}.", PrintSeverity.Normal); else
                   Print(name, $"Could not connect to {router}.", PrintSeverity.Warn);
        }

        private async Task<HttpListenerContext?> FuckCancellationTokens(HttpListener listener) {
            var contextTask = listener.GetContextAsync();
            while (!_cts.IsCancellationRequested) {
                if (contextTask.IsCompleted)
                    return await contextTask;

                if(_cts.IsCancellationRequested) return null;
                await Task.Delay(6);
            }

            return null;
        }

        public void Start() {
            if (_isRunning) { new BotException($"{this.name}:Start", $"The web interface is already running."); return; }
            //if (_cts.IsCancellationRequested) { _cts.Dispose(); _cts = new(); }
            Interlocked.Exchange(ref _is_running, 1);
            if (_isRunning) Print(name, "Started WebInterface.", PrintSeverity.Debug);

            _http_socket = new();
            _http_socket.Prefixes.Add($"{_local_addr}:{_local_port}/api/");
            _http_socket.Prefixes.Add($"{_local_addr}:{_local_port}/sse/"); // 'Command Channel' for the api
            _http_socket.Prefixes.Add($"{_local_addr}:{_local_port}/sse/obs_events/");

            _http_socket.Start();
            if(_http_socket.IsListening && _isRunning) Print(name, "Started Socket.", PrintSeverity.Debug);
            Task.Run(async () => {
                //Task<HttpListenerContext> socketMsg;
                //HttpListenerContext context;
                //Task? TaskTriggered; // I really don't know if this is wise?
                if (_isRunning) Print(name, "Aboot to enter the loop of doom.", PrintSeverity.Debug);
                while (_isRunning) {
                //while (!_cts.IsCancellationRequested) {
                    try {
                        HttpListenerContext? context = await FuckCancellationTokens(_http_socket);
                        //socketMsg = _http_socket.GetContextAsync(); // remove
                        if (context == null) throw new BotException(name, $"Context went null, while listening for messages from _http_socket_listener");
                        //TaskTriggered = await Task.WhenAny(Task.Delay(Timeout.Infinite, _cts.Token), socketMsg);// remove
                        //if (TaskTriggered != socketMsg) break;// remove

                        //context = await socketMsg;// remove

                        //HttpListenerContext context = await _http_socket.GetContextAsync();

                        if (context.Request.Url!.AbsolutePath.Contains("sse"))
                            if (context.Request.Url.AbsolutePath.Contains("obs_events")) { if (OBSHealthy) Print(name, $"Connection to OBS is already esthablief", PrintSeverity.Debug); else _ = StartSSEAsync(context, Route.OBS); } else { _ = StartSSEAsync(context, Route.SSE); }
                        else // come alive
                            _router.Route(context);
                    } catch (Exception ex) {
                        new BotException($"{this.name}:Start", $"Unable to handle incoming request. (Socket: {_http_socket.IsListening})", ex);
                        Shutdown();
                    }
                }
                Print($"{this.name}:_HttpListener", $"WebUI end", PrintSeverity.Normal);
            });
            Print(name, $"WebUI started.", PrintSeverity.Normal);
            //LaunchBrowser("http://localhost:0/"); // I'll handle this later.
        }

        /*public async Task Stop() {
            if (!_isRunning) { Print($"{this.name}:Stop", $"The WebUI is not running.", PrintSeverity.Warn); return; }

            if (_is_SSE_connected && _SSEContext != null)
                await _router.SendSSEAsync(_SSEContext, new { data = new { message = "disconnect", time = DateTime.UtcNow } }.Stringify());
            if (_is_OBSSSE_connected && _OBSSSEContext != null)
                await _router.SendSSEAsync(_OBSSSEContext, new { data = new { message = "disconnect", time = DateTime.UtcNow } }.Stringify());

            Shutdown();
        }*/

        private void Shutdown() {
            _is_SSE_connected = false;
            _is_OBSSSE_connected = false;
            _OBSSSEContext = null;
            _SSEContext = null;

            //_router.Shutdown();

            _cts.Cancel();
            _cts.Dispose();
            Interlocked.Exchange(ref _is_running, 0);
            if (_http_socket != null && _http_socket.IsListening) _http_socket.Stop();
            try { _http_socket?.Close(); } catch { new BotException(name, $"Yeah shit be weird."); }
            Print(name, $"Shutdown WebUI.", PrintSeverity.Debug);
        }

        public void LaunchBrowser(string url) {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" {url}") { CreateNoWindow = true });
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                } else {
                    throw new Exception("Unable to Launch System Browser.");
                }
            } catch {
                throw new BotException($"{this.name}:BrowserLauncher", $"Unable to launch a browser from ze program.");
            }
        }

        public bool Open => _isRunning;
        private bool _isRunning => Interlocked.CompareExchange(ref _is_running, 0, 0) == 1;

        // cleanup shit in down time when the bot is not running
        public void Dispose() {
            /*if (_http_socket != null) {
                _http_socket?.Stop();
                _http_socket?.Close();
                _http_socket = null;
            }*/

            _is_running = Interlocked.Exchange(ref _is_running, 0);
            _socket.Dispose();
            _cts.Dispose();
        }
    }
}
