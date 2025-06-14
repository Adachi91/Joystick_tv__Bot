using ShimamuraBot.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShimamuraBot.Web.Api {
    ///<remarks>This class must remain stateless, please!</remarks>
    internal class Router {
        private string name = "Router";

        #region RouterV2
        #region api_routing
        private Regex api_path = new Regex(@"\s(.*\/)\s");

        public async Task<HttpStatusCode?> RouteAsync(string request, StreamReader reader, StreamWriter writer) {
            Match api_path_match = api_path.Match(request);
            string matchedPath = api_path_match.Groups[1].Value;
            
            switch(matchedPath) {
                case "/api/ping/":
                    await SendPongAsync(writer); // should send pong back to client that accessed api endpoint via SSE. ping, and trigger a waiting response for pong.
                    break;
                case "/api/stats/":
                    break;
                default:
                    new BotException(name, $"Invalid path was given."); // I want to log the path here, but I need to make sure this doesn't go sideways fast.
                    break;
            }

            if (request.StartsWith("GET /api/ping")) {
                await SendPongAsync(writer);
                return HttpStatusCode.OK;
            }

            return null;
        }
        #endregion


        #region SSE_Handling
        public async Task<Route> RouteAsync(Route route, StreamReader reader, StreamWriter writer) {
            switch(route) {
                case Route.OBS:
                    await SendResponse_SSEAsync(writer);
                    return Route.OBS;
                case Route.SSE:
                    await SendResponse_SSEAsync(writer);
                    return Route.SSE;
                case Route.API:
                    throw new BotException(name, "Wrong overload? You can not use this overload for API routing.");
                default:
                    throw new BotException(name, $"Invalid route type. ({route})");
            }
        }


        public async Task SendResponse_SSEAsync(StreamWriter writer) {
            //writer context should be setup after this and can sit in the Dictionary and wait.
            await writer.WriteAsync("HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nAccess-Control-Allow-Headers: Content-Type\r\nCache-Control: no-cache\r\nConnection: keep-alive\r\n\r\n");
        }

        public async Task SendResponse_NotFoundAsync(StreamWriter writer) => await writer.WriteLineAsync($"{HttpHeaderPlainNotFound}404 - Not Found");

        /// <summary>for debug only !</summary>
        /// <remarks>This is to monitor ping/pong to make sure it's functioning correctly.</remarks>
        /// <param name="writer"></param>
        public async Task SendPongAsync(StreamWriter writer) => await writer.WriteLineAsync($"{HttpHeaderJsonOk}{new { @event = "pong", time = DateTime.Now }.Stringify()}");

        private string HttpHeaderJsonOk => "HTTP/1.1 200 OK\r\nContent-Type: application/json\r\n\r\n";
        private string HttpHeaderJsonNotFound => "HTTP/1.1 404 OK\r\nContent-Type: application/json\r\n\r\n";
        private string HttpHeaderPlainOk => "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n";
        private string HttpHeaderPlainNotFound => "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n";
        #endregion
        #endregion


        #region Server-Sent Events
        [Obsolete("Shits ded, yo.")]
        public enum SSEndPoint {
            OBS,
            SSE
        }


        [Obsolete("shits ded yo")]
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

        [Obsolete("shits ded yo")]
        /// <summary>
        ///  Attempts to send a message to StreamWriter context.
        /// </summary>
        /// <remarks>This only sends the writers context so this method has no idea of the client, weither it be SSE or an API route.<br />Will throw <see cref="ObjectDisposedException"/> If the "<see cref="StreamWriter"/>" is disposed due to TcpListener.Close()</remarks>
        /// <param name="writer">The clients StreamWriter.</param>
        /// <param name="json">The message to send in <b>JSON</b> format.</param>
        /// <exception cref="ObjectDisposedException"></exception>
        public async Task SendSSEAsync(StreamWriter writer, string json) {
            await writer.WriteLineAsync("event: message");
            await writer.WriteLineAsync($"data: {json}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }


        [Obsolete("shits ded yo")]
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
        #endregion


        #region API Routes
        [Obsolete("shits ded yo")]
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

        [Obsolete("shits ded yo")]
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
 *      {type: "new_message"} - on New Message.
 *      {type: "presence"} - on Presence change.
 *      {type: "tip"} - Tip was sent.
 *      {type: "ping+pong"} - ping; request pong via Api
 *      {type: "offline"} - Send when stopping.
 *      {type: "online"} - Send when going live.
 *      {type: "update"} - Get all settings refreshed.
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
 * \/api/joystick/messages - Getter
 * \/api/joystick/presence - Getter
 * \/api/joystick/tipped - Getter ?? This might be best sent over SSE.
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