using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot
{
    internal class TempServer
    {
        private readonly HttpListener listener;

        public TempServer()
        {
            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:8087/");
        }

        public async Task StartAsync()
        {
            listener.Start();
            Console.WriteLine("HTTP listener started on http://127.0.0.1:8087/");

            while (true) {
                try {
                    var context = await listener.GetContextAsync();

                    // Read the request body
                    var request = context.Request;
                    var requestBody = new StringBuilder();
                    using (var stream = request.InputStream)
                    {
                        byte[] buffer = new byte[request.ContentLength64];
                        int bytesRead = 0;
                        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            requestBody.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }
                    }

                    // Print the request body to the console
                    if (!string.IsNullOrEmpty(requestBody.ToString()))
                        Console.WriteLine("[HTTP Listener]: " + requestBody.ToString());
                    else
                        Console.WriteLine("[HTTP Listener]: No body was sent back.");

                    // Display the URL used by the client.
                    Console.WriteLine("URL: {0}", request.Url.OriginalString);
                    Console.WriteLine("Raw URL: {0}", request.RawUrl);
                    Console.WriteLine("Query: {0}", request.QueryString);

                    // Display the referring URI.
                    Console.WriteLine("Referred by: {0}", request.UrlReferrer);

                    //Display the HTTP method.
                    Console.WriteLine("HTTP Method: {0}", request.HttpMethod);
                    //Display the host information specified by the client;
                    Console.WriteLine("Host name: {0}", request.UserHostName);
                    Console.WriteLine("Host address: {0}", request.UserHostAddress);
                    Console.WriteLine("User agent: {0}", request.UserAgent);

                    /*NameValueCollection aaa = new NameValueCollection(request.QueryString);

                    foreach(var a in aaa)
                    {
                        Console.WriteLine($"This was in aaa :: {a.ToString()}");
                    }

                    if (aaa.AllKeys.Contains("code"))
                    {
                        Console.WriteLine("CODE WAS FOUND attempting extraction...");
                        
                        for(int i = 0; i < aaa.Count; i++)
                        {
                            if(aaa.Keys.Get(i).ToString() == "code")
                            {
                                var b = aaa.GetValues(i);
                                Console.WriteLine(b.ToString());
                            }
                        }
                    } else
                    {
                        Console.WriteLine("aaa.allkeys.contains failed");
                    }*/

                    // Send a response
                    var response = context.Response;    
                    response.ContentType = "text/html";
                    var responseBytes = Encoding.UTF8.GetBytes("<!DOCTYPE html><html lang=\"en\"><head><title>Authorization Successful</title><style>html,body{background-color:#1c1b22;color:#fff;} h1 {margin:auto; text-align:center; padding-top:5rem;}</style></head><body><h1>Request was Successful. You may close this page now.</h1></body></html>");
                    response.OutputStream.Write(responseBytes, 0, responseBytes.Length);
                    response.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[HTTP Listener Error]: " + ex.Message);
                }
            }
        }

        public void Stop()
        {
            listener.Stop();
            Console.WriteLine("HTTP listener stopped");
        }
    }
}
