using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static ShimamuraBot.Program;

namespace ShimamuraBot
{
    public static class envManager
    {
        /// <summary>
        ///  Loads the environment file
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void load()
        {
            string tmp = null; //hold the long in a string for this type of switch to work.
            string _logging = null;
            foreach (var line in File.ReadAllLines(ENVIRONMENT_PATH)) {
                var split = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
                //int a = line.IndexOf('=');
                //var split = line.Split()

                if (split.Length != 2) continue;

                //Environment.SetEnvironmentVariable(split[0], split[1]);
                //I decided not to use Enviroment Variables because it isn't imediately clear via code what the variable names are.

                var envKey = split[0] switch {
                    "HOST" => HOST = split[1],
                    "CLIENT_ID" => CLIENT_ID = split[1],
                    "CLIENT_SECRET" => CLIENT_SECRET = split[1],
                    "WSS_HOST" => WSS_HOST = split[1],
                    "JWT" => APP_JWT = split[1],
                    "JWT_REFRESH" => APP_JWT_REFRESH = split[1],
                    "JWT_EXPIRE" => tmp = split[1], //TODO extract from JWT and delete this entry.
                    "LOGGING" => _logging = split[1],
                    _ => throw new Exception("The Enviroment Keys in are not structured properly in the .env file\nThe minimum is required\nHOST=HOST_URL\nCLIENT_ID=YOUR_CLIENT_ID\nCLIENT_SECRET=YOUR_CLIENT_SECRET\nWSS_HOST=THE_WSS_ENDPOINT\n")
                };
            }

            if (HOST == null || CLIENT_ID == null || CLIENT_SECRET == null || WSS_HOST == null) throw new Exception("One or more values in the environment file was not found\nThe minimum is required\nHOST=HOST_URL\nCLIENT_ID=YOUR_CLIENT_ID\nCLIENT_SECRET=YOUR_CLIENT_SECRET\nWSS_HOST=THE_WSS_ENDPOINT\n");

            ACCESS_TOKEN = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{CLIENT_ID}:{CLIENT_SECRET}"));
            GATEWAY_IDENTIFIER = new { channel = "GatewayChannel" };
            //WSS_GATEWAY = $"{WSS_HOST}?token={ACCESS_TOKEN}"; //this needs to be set where JWT is handled.
            if (!string.IsNullOrEmpty(tmp)) APP_JWT_EXPIRY = Convert.ToInt64(tmp);
            if(!string.IsNullOrEmpty(_logging)) LOGGING_ENABLED = Convert.ToBoolean(_logging);
        }

        /// <summary>
        ///  Writes Token information to the environment file
        /// </summary>
        /// <param name="logging">(Optional)Boolean - Save all client events to log file.</param>
        public static void write(bool logging = false)
        {
            bool JWT_SET = false;

            List<string> env = File.ReadAllLines(ENVIRONMENT_PATH).ToList();

            for (int i = 0; i < env.Count; i++) {
                if (env[i].StartsWith("JWT=")){
                    JWT_SET = true;
                    env[i] = "JWT=" + APP_JWT;
                } else if (env[i].StartsWith("JWT_REFRESH=")) {
                    env[i] = "JWT_REFRESH=" + APP_JWT_REFRESH;
                } else if (env[i].StartsWith("JWT_EXPIRE=")) {
                    env[i] = "JWT_EXPIRE=" + APP_JWT_EXPIRY.ToString();
                } else if (env[i].StartsWith("LOGGING=")) {
                    env[i] = "LOGGING=" + logging;
                }
            }

            if (!JWT_SET) {
                env.Add("JWT=" + APP_JWT);
                env.Add("JWT_REFRESH=" + APP_JWT_REFRESH);
                env.Add("JWT_EXPIRE=" + APP_JWT_EXPIRY.ToString());
                env.Add("LOGGING=" + logging);
            }

            File.WriteAllLines(ENVIRONMENT_PATH, env);
        }
    }
}
