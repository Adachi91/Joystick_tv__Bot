﻿using System;
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
using System.Net.Http;
using System.Net.Http.Headers;

namespace ShimamuraBot
{
    internal class HTTPServer
    {
        private readonly HttpListener listener;
        private bool _Started { get; set; } = false;
        private void Print(string msg, int lvl) => events.Print(msg, lvl);
        private events.OAuthClient _OAuthPtr;

        private CancellationTokenSource cts = new CancellationTokenSource();

        /// <summary>
        /// Constructor, Creates new HttpListener, and sets Class obj ptr of OAuth
        /// </summary>
        public HTTPServer(events.OAuthClient OAuthPtr) {
                Print("[HTTPServer]: Constructed the HTTP Listener", 0);

                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{LoopbackPort}/auth/");
                _OAuthPtr = OAuthPtr;
        }


        public async Task<bool> Start() {
            if (!await PortCheck())
                Task.Run(() => StartAsync(_OAuthPtr, CancellationToken.None));

            while(!_Started) { //Dumbass way to wait for Raising the HTTPListener
                if(listener.IsListening)
                    break;
                
                Thread.Sleep(10);
            }
            return true;
        }


        private async Task<bool> PortCheck()
        {
            var port = LoopbackPort;
            try
            {
                Print($"[HTTPServer]: Checking if port {port} is open", 0);
                using (TcpClient client = new TcpClient()) {
                    IAsyncResult result = client.BeginConnect("127.0.0.1", port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));
                    if (success) {
                        client.EndConnect(result);
                        client.Close();
                        Print($"[HTTPServer]: The port {port} is being used by another program", 3);
                        return true;
                    }
                    client.Close();
                    Print($"[HTTPServer]: {port} is usable", 0);
                    return false;
                }
            }
            //Always fail on exception, because I don't know shit about the port state.
            catch (SocketException ex) {
                if (ex.SocketErrorCode == SocketError.ConnectionRefused) {
                    Print($"[HTTPServer]: 127.0.0.1:{port} was refused. So assuming it's in use", 0);
                    return true; //When is socketerror refusal in a non-usable state e.g. Program:Port (Fuck off) ??
                }
                throw new Exception($"[HTTPServer]: op 0x9D: I really don't know {ex}");
            } catch (Exception ex) {
                Print($"{ex}", 0);
                return true;
            }
        }

        /// <summary>
        /// Open the URI to ask user for Authenization of resources.
        /// </summary>
        /// <param name="url">Complete URI including endpoint, and GET queries.</param>
        /// <exception cref="Exception"></exception>
        public void openBrowser(string url) { //Code from ODIC Sample Code on Github.
            Print($"Opening {url.Substring(0, 19)} in your webbrowser for authorization", 1);
            try {
                Process.Start(url);
            }
            /*catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    //MessageBox.Show(noBrowser.Message);
                Print($"[Browser]: {noBrowser.Message}", 3);
            }*/ catch /*(Exception ex)*/ {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    url = url.Replace("&", "^&");
                    //Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{url}\"") { CreateNoWindow = true });
                    //Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    //Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    Process.Start("explorer", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    Process.Start("open", url);
                } else {
                    throw new Exception("Unable to Launch System Browser.");
                }
                //Print($"[Process.Start]: Exception occured: {ex}", 3);
            }
        }

        /*
         * The only use case for this right now is getting OAuth token
         * it needs to be reusable
         */

        //Currently thread blocking, so yeah mainloop idea might not have been so dumb.
        /// <summary>
        /// Loopback listener, Main Thread -> Listener -> Data -> StopListening -> Send Main Thread.InstanceEvents
        /// </summary>
        /// <param name="OAuthPtr">Pointer to Constructed OAuth Class</param>
        /// <returns></returns>
        private async Task StartAsync(events.OAuthClient OAuthPtr, CancellationToken Token)
        {
            listener.Start();
            _Started = true;
            Print($"[HTTPServer]: Started on http://127.0.0.1:{Program.LoopbackPort}/auth/", 1);
            

            while (!Token.IsCancellationRequested) {
                try
                {
                    var context = await listener.GetContextAsync();
                    var request = context.Request;
                    var requestBody = await new StreamReader(request.InputStream).ReadToEndAsync();

                    if (!string.IsNullOrEmpty(request.QueryString["state"]))
                        if (request.QueryString["state"] == OAuthPtr.State)
                            if (!string.IsNullOrEmpty(request.QueryString["code"]))
                                OAuthPtr.OAuthCode = request.QueryString["code"];
                            else
                                Print("[HTTPServer]: Unable to retrieve code for OAuth flow", 3);
                        else
                            Print("[HTTPServer]: The nounce returned by the Host was not a match! MITM detected", 3); //I honestly do not think this can ever proc, as it's TLS and even evil twin would not be able to pass it.


                    File.WriteAllText("oauthcode1.txt", OAuthPtr.OAuthCode);

                    var response = context.Response;
                    response.ContentType = "text/html";
                    var responseBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html lang=\"en\"><head><title>Authorization Successful</title><style>html,body{background-color:#1c1b22;color:#fff;} h1 {margin:auto; text-align:center; padding-top:5rem;}</style></head><body><h1>Request was Successful. You may close this page now.</h1></body></html>");
                    response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    response.Close();

                    if (response.StatusCode == 200)
                        cts.Cancel();

                } catch (HttpListenerException ex) {
                    if (ex.ErrorCode == 995) {
                        Print($"[HTTPServer]: Has been stopped", 1);
                        return;
                    }
                    Print($"[HTTPServer]: Not 995 ErrorCode. {ex}", 3);
                } catch (Exception ex) { Print($"[HTTPServer]: {ex}", 3); }
            }

            Print($"[HTTPServer]: Shutting down", 0);
            listener.Stop();
            Task.Delay(1000).Wait();
        }

        /// <summary>
        /// Stop the HTTPServer client
        /// </summary>
        public void Stop() {
            if(!_Started) { Print($"[HTTPServer]: Server is not currently running", 0); return; }
            cts.Cancel();
        }
    }
}
