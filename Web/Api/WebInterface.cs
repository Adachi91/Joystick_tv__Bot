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

        public int debug_clients => _clients.Count();

        public async Task<bool> Stop() {
            if (!_isRunning) { Print(name, "WebUI is not running.", PrintSeverity.Debug); return false; }
            Interlocked.Exchange(ref _is_running, 0);

            _cts.Cancel();

            var sw = Stopwatch.StartNew();

            while(true) {
                if(_clients.Count == 0) {
                    _socket.Stop(); // allow clients to disconnect before shutting socket, cancel has been called so will reject new requests.
                    _cts?.Dispose();
                    Print(name, $"Shutdown WebUI.", PrintSeverity.Debug);
                    return true;
                }

                if(sw.ElapsedMilliseconds > 2_000) {
                    _socket?.Stop();
                    _cts.Dispose();
                    new BotException($"{name}:Close", "Timed-out while waiting for WebUI to shutdown, forcefully closed connections.");
                    return true;
                }

                await Task.Delay(1);
            }
        }


        #region connection_building
        private async Task AcceptConnetionAsync() { // This needs to ignore any new connections when stop has been called.
            while (!_cts.IsCancellationRequested) {
                var client = await _socket.AcceptTcpClientAsync(_cts.Token);
                _ = HandleClientsAsync(client);

                await Task.Delay(1);
            }
        }


        private async Task HandleClientsAsync(TcpClient client) {
            using (var networkStream = client.GetStream())
            using (var reader = new StreamReader(networkStream))
            using (var writer = new StreamWriter(networkStream) { AutoFlush = true }) {
                string? request = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(request)) return;
                if (!_isRunning) return; // Tink u for ur service.

                Print(name, $"Received request: {request}", PrintSeverity.Debug); // path - GET * this will tell me what it looks like, I'm assuming a full request header.
                Route result;
                // TODO: Move to switch and regex.
                if (request.Contains("/sse/obs_events")) {
                    if (_clients.ContainsKey(Route.OBS)) { new BotException(name, "OBS is already in the connection pool."); return; } // bounce it, if already have ctx

                    result = await _router.RouteAsync(Route.OBS, reader, writer);
                    if (AddClient(Route.OBS, writer)) {
                        Print(name, $"Added {result} to the connection pool.", PrintSeverity.Debug);
                        await KeepAlive(result);
                    }
                } else if (request.Contains("/sse")) {
                    if (_clients.ContainsKey(Route.SSE)) { new BotException(name, "Command Channel is already in the connection pool."); return; }
                    result = await _router.RouteAsync(Route.SSE, reader, writer);

                    if (AddClient(Route.SSE, writer)) {
                        Print(name, $"Added {result} to the connection pool.", PrintSeverity.Debug);
                        await KeepAlive(result);
                    }
                } else {
                    throw new NotImplementedException("API Routing");
                }
            }
        }

        private async Task KeepAlive(Route route) {
            try {
                while (true) {
                    //https://stackoverflow.com/questions/30415012/sse-server-sent-events-client-keep-sending-requests-like-polling
                    if (!_clients.ContainsKey(route)) break;
                    await SendSSEAsync(route, new { type = "ping", time = DateTime.UtcNow }.Stringify());

                    _cts.Token.ThrowIfCancellationRequested();
                    await Task.Delay(1000);
                }
            } catch (OperationCanceledException) {
            } catch (IOException) {
                Print(name, $"{route} has disconnected.", PrintSeverity.Debug);
            } catch (Exception ex) {
                new BotException(name, $"Unhandled exception in {route} connection.", ex);
            } finally {
                RemoveClient(route);
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
        private async Task<bool> SendSSEAsync(Route route, string json) {
            if (json.Length < 2) { new BotException(name, $"Tried to send an empty message to {route}."); return true; } // maybe some kind of validation?

            if (_clients.TryGetValue(route, out var writer)) {
                try {
                    //await _router.SendSSEAsync(writer, json);
                    await writer.WriteLineAsync("event: message");
                    await writer.WriteLineAsync($"data: {json}");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                } catch (ObjectDisposedException) { // Dead stream.
                    Print(name, $"{route} has failed a request, and is being removed from the connection pool.", PrintSeverity.Warn);
                    if (!RemoveClient(route))
                        throw new BotException(name, $"Unable to remove {route} from the connection pool.");
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

        [Obsolete("shits ded yo")]
        private void Shutdown() {

            //_router.Shutdown();

            _cts.Cancel();
            _cts.Dispose();
            Interlocked.Exchange(ref _is_running, 0);
            if (_http_socket != null && _http_socket.IsListening) _http_socket.Stop();
            try { _http_socket?.Close(); } catch { new BotException(name, $"Yeah shit be weird."); }
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
            //_is_running = Interlocked.Exchange(ref _is_running, 0);
            _socket?.Dispose();
            _cts?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
