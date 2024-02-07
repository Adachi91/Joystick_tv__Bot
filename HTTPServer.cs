using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShimamuraBot
{
    internal class HTTPServer
    {
        private readonly HttpListener listener;
        private OAuthClient _OAuthPtr;

        private CancellationTokenSource cts;
        private CancellationToken cancelToken;

        /// <summary>
        ///  Constrcuts a new HTTPListener client
        /// </summary>
        /// <param name="OAuthPtr">Constructed OAuth class pointer</param>
        public HTTPServer(OAuthClient OAuthPtr) {
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{LoopbackPort}/auth/");
            _OAuthPtr = OAuthPtr;

            Print("[HTTPServer]: Constructed the HTTP Listener", 0);
        }


        /// <summary>
        ///  Starts the HTTPListener for OAuth2 Loopback Flow
        /// </summary>
        /// <returns></returns>
        public void Start() { //passed test
            if (PortCheck()) {
                cts = new CancellationTokenSource();
                cancelToken = cts.Token;

                var state = OAuthClient.Generatestate();

                Task.Run(() => StartAsync(_OAuthPtr, cancelToken));
                openBrowser(_OAuthPtr.Auth_URI.ToString() + $"?client_id={CLIENT_ID}&scope=bot&state={_OAuthPtr.State}");
            }
        }


        /// <summary>
        ///  Force Stop the HTTPListener
        /// </summary>
        public void Stop() { //passed test
            if (!listener.IsListening) { Print($"[HTTPServer]: Server is not currently running", 2); return; }
            cts.Cancel();
        }


        /// <summary>
        ///  Attempt to see if port for loopback is in use currently.
        /// </summary>
        /// <returns>(Bool) True:Usable, False:SocketException/In use</returns>
        /// <exception cref="SocketException"></exception>
        private bool PortCheck() { //passed test
            try {
                Print($"[HTTPServer]: Checking if port {LoopbackPort} is open", 0);

                using (TcpListener portListener = new TcpListener(IPAddress.Loopback, LoopbackPort)) {
                    portListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    portListener.Start();
                    portListener.Stop();
                    Print($"[HTTPServer]: Port {LoopbackPort} is free", 0);
                    return true;
                }
            } catch (SocketException ex) {
                Print($"[HTTPServer]: SocketException while checking Port({LoopbackPort}): {ex.Message}", 3);
                return false;
            } catch (Exception ex) {
                Print($"[HTTPServer]: Exception while checking port({LoopbackPort}): {ex.Message}", 3);
                return false;
            }
        }


        /// <summary>
        /// Open the URI to ask user for Authorization of resources.
        /// </summary>
        /// <param name="url">Complete HOST URI</param>
        /// <exception cref="Exception"></exception>
        private void openBrowser(string url) { //Code from ODIC Sample Code on Github.
            Print($"Opening {HOST} in your webbrowser for authorization", 1);
            try {
                Process.Start(url);
            } catch {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" {url}") { CreateNoWindow = true });
                    //firefox updated during writing the code and since I had firefox open, the old executable was in memory but a new executable was on disk
                    //the program would not launch a new instance of "firefox" because they were mismatching? or something else that I do not understand fully.
                    //upon restarting firefox it works
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                } else {
                    throw new Exception("Unable to Launch System Browser.");
                }
            }
        }


        /// <summary>
        ///  Listens for HTTP events on localhost:port
        /// </summary>
        /// <param name="OAuthPtr">Pointer to constructed OAuth Class</param>
        /// <param name="Token">Cancellation Token</param>
        /// <returns>null</returns>
        private async Task StartAsync(OAuthClient OAuthPtr, CancellationToken Token) { //passed test
            listener.Start();
            Print($"[HTTPServer]: Started on http://127.0.0.1:{LoopbackPort}/auth/", 1);


            try {
                while (!Token.IsCancellationRequested) {
                    var contextTask = listener.GetContextAsync();
                    var completedTask = await Task.WhenAny(contextTask, Task.Delay(Timeout.Infinite, Token));

                    if (completedTask != contextTask) break;

                    var listenerCtx = await contextTask;

                    using (var reader = new StreamReader(listenerCtx.Request.InputStream)) {
                        var requestBody = await reader.ReadToEndAsync();

                        if (!string.IsNullOrEmpty(listenerCtx.Request.QueryString["state"]))
                            if (listenerCtx.Request.QueryString["state"] == OAuthPtr.State)
                                if (!string.IsNullOrEmpty(listenerCtx.Request.QueryString["code"]))
                                    OAuthPtr.OAuthCode = listenerCtx.Request.QueryString["code"];
                                else
                                    throw new Exception($"[HTTPServer]: Unable to Retrieve Authorization code from {HOST}");
                            else
                                throw new Exception($"[HTTPServer]: The nounce returned by {HOST} was not a match");
                    }

                    listenerCtx.Response.ContentType = "text/html";
                    await using (var writer = new StreamWriter(listenerCtx.Response.OutputStream))
                        await writer.WriteAsync("<!DOCTYPE html><html lang=\"en\"><head><title>Authorization Successful</title><style>html,body{background-color:#1c1b22;color:#fff;} h1 {margin:auto; text-align:center; padding-top:5rem;}</style></head><body><h1>Request was Successful. You may close this page now.</h1></body></html>");

                    Print($"[HTTPServer]: Successfully got OAuth2 code from {HOST}", 1);
                    cts.Cancel();
                }
            } catch (Exception ex) {
                Print($"[HTTPServer]: {ex.Message}", 3);
            } finally {
                if (listener.IsListening)
                    listener.Stop();
                Print($"[HTTPServer]: Shutting down.", 1);
            }
        }
    }
}
