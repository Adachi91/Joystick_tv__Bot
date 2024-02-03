using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
//using System.Net.Http;
//using System.Net.Http.Headers;

namespace ShimamuraBot
{
    internal class HTTPServer
    {
        private readonly HttpListener listener;
        private OAuthClient _OAuthPtr;

        private CancellationTokenSource cts = new CancellationTokenSource();
        private CancellationToken cancelToken;

        /// <summary>
        ///  Constrcuts a new HTTPListener client
        /// </summary>
        /// <param name="OAuthPtr">Constructed OAuth class pointer</param>
        public HTTPServer(OAuthClient OAuthPtr) {
            cancelToken = cts.Token;
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{LoopbackPort}/auth/");
            _OAuthPtr = OAuthPtr;

            Print("[HTTPServer]: Constructed the HTTP Listener", 0);
        }


        /// <summary>
        ///  Checks if port is in use if not it will start the HTTPListener
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Start() {
            if (!await PortCheck())
                Task.Run(() => StartAsync(_OAuthPtr, cancelToken));
            else
                return false;

            /*while(!_Started) { //Dumbass way to wait for Raising the HTTPListener
                if(listener.IsListening)
                    break;
                
                Thread.Sleep(10);
            }*/
            return true;
        }


        /// <summary>
        /// Stop the HTTPServer client
        /// </summary>
        public void Stop()
        {
            if (!listener.IsListening) { Print($"[HTTPServer]: Server is not currently running", 2); return; }
            cts.Cancel();
        }


        /// <summary>
        ///  Attempt to see if port for loopback is in use currently.
        /// </summary>
        /// <returns>boolean</returns>
        /// <exception cref="Exception"></exception>
        private async Task<bool> PortCheck()
        {
            try {
                Print($"[HTTPServer]: Checking if port {LoopbackPort} is open", 0);
                using (TcpClient client = new TcpClient()) {
                    IAsyncResult result = client.BeginConnect("127.0.0.1", LoopbackPort, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (success) {
                        client.EndConnect(result);
                        client.Close();
                        Print($"[HTTPServer]: The port {LoopbackPort} is being used by another program", 3);
                        return true;
                    }
                    client.Close();
                    Print($"[HTTPServer]: {LoopbackPort} is usable", 0);
                    return false;
                }
            }
            //Always fail on exception, because I don't know shit about the port state.
            catch (SocketException ex) {
                if (ex.SocketErrorCode == SocketError.ConnectionRefused) {
                    Print($"[HTTPServer]: 127.0.0.1:{LoopbackPort} was refused. So assuming it's in use", 0);
                    return true;
                }
                throw new Exception($"[HTTPServer]: op 0x9D: I really don't know {ex}");
            } catch (Exception ex) {
                Print($"{ex}", 0);
                return true;
            }
        }


        /// <summary>
        /// Open the URI to ask user for Authorization of resources.
        /// </summary>
        /// <param name="url">Complete HOST URI</param>
        /// <exception cref="Exception"></exception>
        public void openBrowser(string url) { //Code from ODIC Sample Code on Github.
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

        //Currently thread blocking, so yeah mainloop idea might not have been so dumb.

        /// <summary>
        ///  Listens for HTTP events on localhost:port
        /// </summary>
        /// <param name="OAuthPtr">Pointer to constructed OAuth Class</param>
        /// <param name="Token">Cancellation Token</param>
        /// <returns>null</returns>
        private async Task StartAsync(OAuthClient OAuthPtr, CancellationToken Token)
        {
            listener.Start();
            Print($"[HTTPServer]: Started on http://127.0.0.1:{LoopbackPort}/auth/", 1);

            try
            {
                Task<HttpListenerContext> context = listener.GetContextAsync();
                Task cancelRequest = Task.Delay(Timeout.Infinite, Token);
                Task taskDone = await Task.WhenAny(context, cancelRequest);

                if (taskDone == cancelRequest) { Print($"[HTTPServer]: Cancellation received. Shut down Complete", 1); listener.Stop(); return; }

                var ctxx = await context;
                var request = ctxx.Request;
                var requestBody = await new StreamReader(request.InputStream).ReadToEndAsync(Token);

                if (!string.IsNullOrEmpty(request.QueryString["state"]))
                    if (request.QueryString["state"] == OAuthPtr.State)
                        if (!string.IsNullOrEmpty(request.QueryString["code"]))
                            OAuthPtr.OAuthCode = request.QueryString["code"];
                        else
                            Print($"[HTTPServer]: Unable to Retrieve Authorization code from {HOST}", 3);
                    else
                        Print($"[HTTPServer]: The nounce returned by {HOST} was not a match!", 3); //I honestly do not think this can ever proc, as it's TLS and even evil twin would not be able to pass it.


                var response = ctxx.Response;
                response.ContentType = "text/html";
                var responseBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html lang=\"en\"><head><title>Authorization Successful</title><style>html,body{background-color:#1c1b22;color:#fff;} h1 {margin:auto; text-align:center; padding-top:5rem;}</style></head><body><h1>Request was Successful. You may close this page now.</h1></body></html>");
                response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                response.Close();

                if (response.StatusCode == 200)
                {
                    Print($"[HTTPServer]: Got OAuth code from {HOST}", 0);
                    cts.Cancel();
                }

            }
            catch (HttpListenerException ex) {
                if (ex.ErrorCode == 995) { Print($"[HTTPServer]: Stopped unexpectedly", 2); return; }
                Print($"[HTTPServer]: Stopped unexpectedly. Error: {ex}", 3);
            }
            catch (Exception ex) { Print($"[HTTPServer]: Stopped unexpectedly. Error: {ex}", 3); }

            Print($"[HTTPServer]: Successfully received Authorization from {HOST}. Job Complete Shutting Down listener...", 1);
            listener.Stop();
            Task.Delay(1000).Wait();


            OAuthPtr.callmewhateverlater(1);
        }
    }
}
