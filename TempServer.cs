using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot
{
    internal class TempServer
    {
        private readonly HttpListener listener;
        private bool stopReuqest { get; set; } = false;
        private void Print(string msg, int lvl) => events.Print(msg, lvl);

        /// <summary>
        /// Construct and start listening on localhost:port for incoming OAuth redirects
        /// </summary>
        public TempServer(events.OAuthClient OAuthPtr) {
            if (!events.OAuthClient.VerifyPortAccessibility(Program.LoopbackPort)) {
                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{Program.LoopbackPort}/auth/");

                Task.Run(() => StartAsync(OAuthPtr));
            } else
                Print($"Unable to start Authorization flow. Port {Program.LoopbackPort} is being used by another program,,", 3);
        }

        /// <summary>
        /// Loopback listener, Main Thread -> Listener -> Data -> StopListening -> Send Main Thread.InstanceEvents
        /// </summary>
        /// <param name="OAuthPtr">Pointer to Constructed OAuth Class</param>
        /// <returns></returns>
        public async Task StartAsync(events.OAuthClient OAuthPtr)
        {
            listener.Start();
            Print($"HTTPListener started on http://127.0.0.1:{Program.LoopbackPort}/auth/", 1);
            
            while (listener.IsListening) {
                try {
                    var context = await listener.GetContextAsync();
                    var request = context.Request;
                    /*var requestBody = new StringBuilder();
                    using (var stream = request.InputStream) {
                        byte[] buffer = new byte[request.ContentLength64];
                        int bytesRead = 0;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0) {
                            requestBody.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }
                    }*/
                    var requestBody = await new StreamReader(request.InputStream).ReadToEndAsync();

                    if (!string.IsNullOrEmpty(request.QueryString["state"]))
                        if (request.QueryString["state"] == OAuthPtr.state)
                            if (!string.IsNullOrEmpty(request.QueryString["code"]))
                                OAuthPtr.code = request.QueryString["code"];
                            else
                                Print("Unable to retrieve code for OAuth flow", 3);
                        else
                            Print("The return state of the Authority was a mismatch or invalid", 3);

                    var response = context.Response;
                    response.ContentType = "text/html";
                    var responseBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html lang=\"en\"><head><title>Authorization Successful</title><style>html,body{background-color:#1c1b22;color:#fff;} h1 {margin:auto; text-align:center; padding-top:5rem;}</style></head><body><h1>Request was Successful. You may close this page now.</h1></body></html>");
                    response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    response.Close();
                    if(response.StatusCode == 200) {
                        stopReuqest = true;
                        listener.Stop();
                        Task.Delay(1000).Wait();
                    }
                } catch (HttpListenerException ex) {
                    if(ex.ErrorCode == 995 && stopReuqest) {
                        //everything is fine, this is fine, the fire is fine.
                        return;
                    }
                    Print($"Unexpected error in TempServer (!995): {ex}", 3);
                } catch (Exception ex)
                {
                    Print($"Unexpected Error in TempServer (Unknown): {ex}", 3);
                }
            }
        }

        /// <summary>
        /// May cause anal leakage, computer fires, house fires, campfires, orgies, your results may vary.
        /// </summary>
        public void Stop() {
            if (listener.IsListening) {
                stopReuqest = true;
                listener.Stop();
                Task.Delay(1000).Wait();
            } else
                Print($"HttpListener is not currently running..", 0);
        }
    }
}
