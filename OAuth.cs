using System;
//using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
//using System.Threading;
using System.Threading.Tasks;
//using System.ComponentModel.Design;

namespace ShimamuraBot
{
    public class OAuthClient
    {
        private string name = "OAuth";
        /// <summary>
        /// THIS IS THE ONMLY THING THE CLASS IS BEING USED FOR RIGHT NOW
        /// </summary>
        private readonly string Authority;
        private readonly Uri RedirectURI;
        private readonly string Scope;
        private readonly string ClientIdentity;
        private readonly string ClienttSecret;
        public readonly Uri Auth_URI;
        public readonly Uri Token_URI;
        public string State;
        public string OAuthCode {
            get { return _oauthCode; }
            set {
                _oauthCode = value;
                _ = Logger.Log("Debug", new string[] { $"OAUTH_DEBUG: '{_oauthCode}'" });
                if (!string.IsNullOrEmpty(_oauthCode) )
                    _ = request_token(1);
            }
        }
        private string _oauthCode;

        /// <summary>
        ///  OAuth Constructor class
        /// </summary>
        /// <param name="_host">The host with scheme</param>
        /// <param name="_client_id">Client Identifier</param>
        /// <param name="_client_secret">Client Secret</param>
        /// <param name="_authorize_uri">OAuth URI</param>
        /// <param name="_token_uri">Token URI</param>
        /// <param name="_redirect_uri">The redirect URI</param>
        /// <param name="_scope">Scope (optional) should be bot</param>
        public OAuthClient(string _host, string _client_id, string _client_secret, string _authorize_uri, string _token_uri, string _redirect_uri, string _scope = "ALLURBASES")
        {
            //if (!_host.ToLower().StartsWith("https://")) {
            if (String.Compare(_host, 0, "https://", 0, 8, StringComparison.OrdinalIgnoreCase) != 0) { // Why? Because I find it hilarious.
                Print(this.name, $"Could not construct the OAuth class. The Scheme detected was not HTTPS", PrintSeverity.Error);
                return;
            }

            Authority = _host;
            ClientIdentity = _client_id;
            ClienttSecret = _client_secret;
            RedirectURI = new Uri(_redirect_uri);

            Auth_URI = new Uri($"{_host}{_authorize_uri}");
            Token_URI = new Uri($"{_host}{_token_uri}");

            Scope = _scope;
        }


        /// <summary>
        /// Generate a nounce "state" for comparison on callback to protect against MiTM attacks, however not required for loopback, still implemented.
        /// </summary>
        /// <returns>String</returns>
        public static string Generatestate() {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rand = new Random();
            return new string(Enumerable.Repeat(chars, 32).Select(s => s[rand.Next(s.Length)]).ToArray());
        }


        /// <summary>
        /// Send a POST request to the host's gateway to request a JWT using HTTPClient
        /// Resource use has been authorized by this point.
        /// </summary>
        /// <param name="type">Type of request - 1:Request Token, 2:Refresh Token</param>
        private async Task<bool> request_token(int type) { // Token valid 10 hours.
            if (JWT.Valid && !JWT.Expired) { Print(this.name, $"Token is valid and not expired.", PrintSeverity.Warn); return false; }

            string endpointParams;

            switch (type) {
                case 1: endpointParams = $"?redirect_uri=unused&code={OAuthCode}&grant_type=authorization_code";
                    break;
                case 2: endpointParams = $"?refresh_token={REFRESH_TOKEN}&grant_type=refresh_token";
                    break;
                default: new BotException(this.name, "Invalid token request type.");
                    return false;
            }

            try {
                using (HttpClient hc = new HttpClient()) {
                    hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", CLIENT_AUTH_HEADER);
                    hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var emptybody = new StringContent("");
                    emptybody.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                    emptybody.Headers.Add("X-JOYSTICK-STATE", State);

                    HttpResponseMessage resp = await hc.PostAsync(new Uri($"{Token_URI}{endpointParams}"), emptybody);

                    if (resp.IsSuccessStatusCode) {
                        string respData = await resp.Content.ReadAsStringAsync();

                        foreach (var header in resp.Headers)
                            if (header.Key == "X-JOYSTICK-STATE" && (string.Join(", ", header.Value) != State))
                                Print(this.name, $"There was a mismatch in the return state from {HOST}. (Received {string.Join(", ", header.Value)} NOT {State})", PrintSeverity.Error);

                        //UpdateValues(respData); // possible race-condition
                        if (type == 2) _ = Logger.Log(name, new string[] { $"Received Refresh Payload:", $"{respData}" });
                        HostAuth _HostAuth = JsonSerializer.Deserialize<HostAuth>(respData);

                        ACCESS_TOKEN = _HostAuth.access_token;
                        //APP_JWT_EXPIRY = Convert.ToInt64(respData.expires_in);
                        REFRESH_TOKEN = _HostAuth.refresh_token;

                        Print(this.name, $"Succesfully received authorization from {HOST}.", PrintSeverity.Debug);

                        // Write new Auth Tokens to .env
                        _ = Task.Run(() => {
                            envManager.write();
                        });

                        return true; // Received Auth & Codes from Host.
                    } else {
                        Print(name, $"Failed to receive authorization from {HOST}. (HTTP {resp.StatusCode})", PrintSeverity.Error);
                        return false;
                    }
                }
            } catch (Exception ex) {
                new BotException(this.name, $"There was an error receiving authorization from {HOST}.", ex);
                return false;
            }
        }

        private class HostAuth {
            public string access_token { get; set; }
            public long? expires_in { get; set; }
            public string token_type { get; set; }
            public string refresh_token { get; set; }
        }

        //deprecated it's stupid it only has a single caller and it should be inside the Task running it not spawning off into fucking a million methods.
        /*private Task UpdateValues(string data) {
            try {
                var respData = JsonSerializer.Deserialize<ResponseDeserialization>(data);
                ACCESS_TOKEN = respData.access_token;
                APP_JWT_EXPIRY = Convert.ToInt64(respData.expires_in);
                REFRESH_TOKEN = respData.refresh_token;

                Print(this.name, $"Succesfully retrieved authorization from {HOST}", PrintSeverity.Debug);
            } catch (Exception ex) {
                new BotException(this.name, $"Error recieving authorization from {HOST}.", ex);
            }
            envManager.write();
        }*/

        public async Task<bool> RefreshToken() => await request_token(2);
    }
}
