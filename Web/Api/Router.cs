using ShimamuraBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot.Web.Api {
    ///<remarks>This class must remain stateless, please!</remarks>
    internal class Router {
        private string name = "Router";

        #region RouterV2

        public enum ApiRoutes {
            Help,
            Ping,
            Noop,
            etc,
            Pong
        }

        public async Task<ApiRoutes?> RouteAsync(string request, StreamReader reader, StreamWriter writer) {
            if(request.StartsWith("GET /api/ping")) {
                await SendPongAsync(writer);
                return ApiRoutes.Pong;
            }

            return null;
        }

        public async Task<WebInterface.Route?> RouteAsync(WebInterface.Route route, StreamReader reader, StreamWriter writer) {
            switch(route) {
                case WebInterface.Route.OBS:
                    await SendResponse_SSEAsync(writer);
                    return WebInterface.Route.OBS;
                case WebInterface.Route.SSE:
                    await SendResponse_SSEAsync(writer);
                    return WebInterface.Route.SSE;
                case WebInterface.Route.API:
                    new BotException(name, "Wrong overload? You can not use this overload for API routing.");
                    return null;
                default:
                    new BotException(name, $"Invalid route type. ({route})");
                    return null;
            }

            /*if (request.StartsWith("GET /api/ping")) {
                await SendPongAsync(writer);
            } else if (request.StartsWith("GET /SSE/OBS")) {
                await SendResponse_SSEAsync(writer);
            } else if(request.StartsWith("GET /SSE")) {
                await SendResponse_SSEAsync(writer);
            }*/
        }


        public async Task SendResponse_SSEAsync(StreamWriter writer) {
            //writer context should be setup after this and can sit in the Dictionary and wait.
            await writer.WriteAsync("HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nAccess-Control-Allow-Headers: Content-Type\r\nCache-Control: no-cache\r\nConnection: keep-alive\r\n\r\n");
        }

        public async Task SendResponse_NotFoundAsync(StreamWriter writer) => await writer.WriteLineAsync("HTTP/1.1 404 Not Found\r\nContent-Type: text/plain\r\n\r\nNot Found");

        /// <summary>
        ///  for debug only !
        /// </summary>
        /// <remarks>This is to monitor ping/pong to make sure it's functioning correctly.</remarks>
        /// <param name="writer"></param>
        /// <returns></returns>
        public async Task SendPongAsync(StreamWriter writer) => await writer.WriteLineAsync($"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n{new { @event = "pong", time = DateTime.Now }.Stringify()}");
        #endregion


        /// <summary>
        ///  Routing for API
        /// </summary>
        /// <param name="context"><see cref="WebInterface._http_socket"/> HTTPListener</param>
        public void Route(HttpListenerContext context) {
            HttpListenerRequest? request = context.Request;

            if(request == null) return;
            if(string.IsNullOrEmpty(request.Url?.AbsolutePath)) return;

            var location = request.Url.AbsolutePath;

            switch(location) {
                case "/ping": // this is for API route
                    SendPong(context);
                    break;
                default:
                    dynamic resp = new {
                        Message = "Not Found",
                        Time = DateTime.UtcNow
                    }.Stringify();
                    SendSSEAsync(context, resp);
                    break;
            }
        }


        #region Server-Sent Events
        public enum SSEndPoint {
            OBS,
            SSE
        }

        /*public Task<bool> _dumbstructor(HttpListenerContext ctx, SSEndPoint endpoint_context) { /// Not so stateless afterall, eh?
            try {
                switch(endpoint_context) {
                    case SSEndPoint.OBS: if (obs_streamreader == null) obs_streamreader = new(ctx.Response.OutputStream); break;
                    case SSEndPoint.SSE: if (apissestreamreader == null) apissestreamreader = new(ctx.Response.OutputStream); break;
                }
            } catch (Exception ex) {
                new BotException($"{name}:_dumbstructor", $"Could not set context for {endpoint_context}.", ex);
                return false;
            }

            return true;
        }*/

        private async Task<bool> WriteStreamAsync(HttpListenerContext ctx, string json) {
            bool successfulfailure = false;

            try {
                using (StreamWriter streamreader = new(ctx.Response.OutputStream)) {
                    await streamreader.WriteLineAsync("event: message");
                    await streamreader.WriteLineAsync($"data: {json}");
                    await streamreader.WriteLineAsync();
                    await streamreader.FlushAsync();
                }
                successfulfailure = true;
            } catch (Exception ex) {
                new BotException($"{this.name}:StreamWriter", "AHHHHHHHHHHHHHHHHH", ex);
            }

            return successfulfailure;
        }

        /// <summary>
        ///  Attempts to send a message to StreamWriter context.
        /// </summary>
        /// <remarks>This only sends the writers context so this method has no idea of the client, weither it be SSE or an API route.</remarks>
        /// <param name="writer">The clients StreamWriter.</param>
        /// <param name="json">The message to send in <b>JSON</b> format.</param>
        /// <returns><see cref="bool"/> True:Failed, False:Success</returns>
        public async Task<bool> SendSSEAsync(StreamWriter writer, string json) { // going to panic at the last level so the caller can maybe try to handle it.
            try {
                await writer.WriteLineAsync("event: message");
                await writer.WriteLineAsync($"data: {json}");
                await writer.WriteLineAsync();
                await writer.FlushAsync();
                return false;
            } catch (Exception ex) {
                new BotException(name, "Error sending SSE Message.", ex);
            }

            return true;
        }

        public async Task<bool> SendSSEAsync(HttpListenerContext context, string json, SSEndPoint endpoint = SSEndPoint.OBS) {
            // Headers should already be set and keep-alive, etc. since I'm passing around the SSE Context.
            try {
                Print(name, $"Sending {json}", PrintSeverity.Debug);
                switch(endpoint) {
                    case SSEndPoint.OBS: return await WriteStreamAsync(context, json);
                    case SSEndPoint.SSE: return await WriteStreamAsync(context, json);
                    default: throw new BotException(name, $"Invalid endpoint.");
                }
            } catch (Exception ex) {
                new BotException($"{this.name}:SendSSEAsync", $"Was unable to send message to SSE. (Context: {endpoint})", ex);
                return false;
            }
        }
        //dumbass hack- figure out what to do, either inhereit webinterface or stfu.
        //public void Shutdown() {
          //  obs_streamreader = null;
            //apissestreamreader = null;
        //}
        #endregion


        #region API Routes
        private async Task<bool> SendAPIMessageAsync(HttpListenerContext context, object payload) {
            HttpListenerResponse response = context.Response;
            response.ContentType = "application/json";

            try {
                using (StreamWriter writer = new StreamWriter(response.OutputStream)) {
                    await writer.WriteLineAsync(payload.ToString());
                    await writer.FlushAsync();
                }
            } catch (Exception ex) {
                new BotException($"{this.name}:SendAPIMessageAsync", $"Unable to send API response. Request: {context?.Request?.Url?.AbsolutePath ?? "null"}", ex);
                return false;
            }
            return true;
        }

        public void SendPong(HttpListenerContext ctx) {
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;

            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            var body = reader.ReadToEnd();
            dynamic message = body.Parse();

            Console.WriteLine($"Ping received? :: {message?.Message}");

            dynamic asdf = new {
                Message = "Pong",
                Time = DateTime.UtcNow
            }.Stringify();

            byte[] buffer = Encoding.UTF8.GetBytes(asdf);
            //response.ContentType = "application/json"; // I think this will kill the SSE connection.
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Write([0x0a, 0x0a], 0, 2); // I think this works.
            response.OutputStream.Close();
        }
        #endregion
    }
}

/*****************************************************************************************************************\
 * 
 * Routes
 * \/sse/obs_events - Server-Sent Events for OBS Studio (This endpoint will initiate a connection to the OBS SSE) - done
 * \/sse - Server-Sent Event Command Channel for the API to push messages to the client. - done
 * 
 *  - Generics for \/api endpoint. -
 * \/api/ping - Ping/Pong endpoint for testing the API.
 * \/api/stats - Endpoint for getting the current statistics of the bot.
 * 
 * - Joystick API -
 * \/api/joystick/settings - Getter Stter
 * \/api/joystick/ban - Setter
 * \/api/joystick/mute - Setter
 * \/api/joystick/banwords - Setter Getter
 * 
 * 
 * 
 * - Generic API -
 * \/api/settings - Getter Setter
 * \/api/settings/modules - Getter Setter
 * \/api/
 * \/api/global/set - This I want to ultimately set title through all API endpoints so I don't have to fucking go to 400 webpages and enter titles.
 * \/api/social/discord - Send Discord Webhook
 * \/api/social/bluesky - o _ o i'm stupid. I looked at docu and couldn't figure out how to do stuff. Help plz dev
 * \/api/social/x - eeeh didn't they start charging for api? idk look
 * \/api/social/linkendin /s
 * \/api/social/stackoverflow /s
 \****************************************************************************************************************/