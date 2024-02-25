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
                if (!string.IsNullOrEmpty(_oauthCode))
                    if (!checkJWT())
                        _ = callmewhateverlater(1);
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
            if (!_host.ToLower().StartsWith("https://")) {
                Print($"[OAuth]: Could not construct the OAuth class. The Scheme detected was not HTTPS", 3);
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
        /// <returns></returns>
        public static string Generatestate() {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random rand = new Random();
            return new string(Enumerable.Repeat(chars, 32).Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        /// <summary>
        ///  Checks to see if you have a valid token
        /// </summary>
        /// <returns>returns true if valid, false if not valid or expired</returns>
        public static bool checkJWT() {
            if (!string.IsNullOrEmpty(APP_JWT))
                if (APP_JWT_EXPIRY - GetUnixTimestamp() >= 900)
                    return true;
                else
                    return false;
            else
                return false;
        }

        /// <summary>
        /// Send a POST request to the host's gateway to request a JWT using HTTPClient
        /// </summary>
        /// <param name="type">Type of grant. 1:authorization_code, 2:refresh_token</param>
        public async Task<bool> callmewhateverlater(int type) {
            //first let's do a few checks

            if (checkJWT()) { Print($"[OAuth]: Already have a valid JWT. Not requesting a new one...", 2); return false; }

            string grantType = "";
            if (type == 1) grantType = "authorization_code";
            else if (type == 2) grantType = "refresh_token";
            else { Print($"[OAuth]: Invalid grant_type provided. The type must be 1:authorization or 2:refresh", 3); return false; }

            using (HttpClient hc = new HttpClient()) {
                hc.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", ACCESS_TOKEN);
                hc.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var emptybody = new StringContent("");
                emptybody.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                emptybody.Headers.Add("X-JOYSTICK-STATE", State);

                HttpResponseMessage resp = await hc.PostAsync(new Uri($"{Token_URI}?redirect_uri=unused&code={OAuthCode}&grant_type={grantType}"), emptybody);

                if (resp.IsSuccessStatusCode) {
                    Print($"[HTTPClient]: Successfully received response from {HOST}", 0);
                    string respData = await resp.Content.ReadAsStringAsync();

                    foreach (var header in resp.Headers)
                        if (header.Key == "X-JOYSTICK-STATE")
                            if (string.Join(", ", header.Value) != State)
                                Print($"[HTTPClient]: There was a mismatch in the return state from {HOST}", 3);

                    UpdateValues(respData);
                    return true;
                } else {
                    Print($"[HTTPClient]: Failed to retrieve JWT from {HOST} with HTTP status of {resp.StatusCode}", 3);
                    return false;
                }
            }
        }

        private class ResponseDeserialization {
            public string access_token { get; set; }
            public long? expires_in { get; set; }
            public string refresh_token { get; set; }
        }

        public void UpdateValues(string data) {
            try {
                var respData = JsonSerializer.Deserialize<ResponseDeserialization>(data);
                APP_JWT = respData.access_token;
                APP_JWT_EXPIRY = Convert.ToInt64(respData.expires_in);
                APP_JWT_REFRESH = respData.refresh_token;

                Print($"[JWT Parser]: Succesfully retrieved JWT from {HOST}", 0);
            } catch (Exception ex) {
                Print($"[JWT Parser]: There was an error parsing the response from {HOST}\n{ex}\n", 3);
            }
            envManager.write();
        }

        public void checkTimestamp()
        {
            if (string.IsNullOrEmpty(APP_JWT)) return;

            Print($"[Timer]: Attempting to refresh JWT...", 0);

            if(GetUnixTimestamp() - APP_JWT_EXPIRY <= 60) {
                callmewhateverlater(2);
            }
        }
    }
}
