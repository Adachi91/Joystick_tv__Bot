﻿using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
//using static System.Runtime.InteropServices.JavaScript.JSType;
using ShimamuraBot.Classes;
using System.Runtime.InteropServices;

namespace ShimamuraBot
{
    class Program
    {
        //if you decode it let me know -adachi
        //public const string EASTER_EGG = "GAJ9MDCDIDEAHDTC9D9DEAADTCEAXCHDLAGDEAPC9DFDXCVCWCHDQAJ9HDTC9D9DEAADTCEASBLAADEAUCCDFDVCXCJDTCBDEAHDCDBDXCVCWCHDQAJ9QCIDHDEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCEABDCDKDQAJ9SBLAADEAWCCD9DSCXCBDVCEAIDDDEAPCEA9DXCVCWCHDQAJ9RCWCPCGDXCBDVCEACDIDHDEAHDWCTCEASCPCFDZCBDTCGDGDEAXCBDGDXCSCTCQAJ9RCPCIDGDTCEABDCDQCCDSCMDEARCPCBDEAGDPCJDTCEAADTCSAJ9GA";
        public static Int16 LoopbackPort = 8087;
        private static WebsocketClient wss;
        private static string name = "Shimamura";

        public static string HOST;          // <=== base host
        public static string CLIENT_ID;     // <=== ApplicationID to form basic auth
        public static string CLIENT_SECRET; // <=== Client Secret to form basic auth
        public static string WSS_HOST;      // <=== Host
        //public static string WSS_ENDPOINT;// <=== Websocket Endpoint (WSS_HOST + AUTH_HEADER)
        //public static string CLIENT_AUTH_HEADER; // <=== B64 Basic Auth
        public static string ACCESS_TOKEN;  // <=== Web Token
        public static string REFRESH_TOKEN; // <=== Refresh Token
        public static string ENVIRONMENT_PATH;
        public static bool DEBUGGING_ENABLED = false;
        public static VNyan vNyan;
        private static Twitch _Twitch;
        public static bool IS_WIN = true; // I'm so sorry *Nix and MacOS users. I simply don't want to include 3rd party libraries outside of .net :| TODO: Figure out ALSA api

        public const string HISTORY_PATH = @"shimamura.log";
        public static bool LOGGING_ENABLED = false;

        //private static SynchronizationContext mainThreadContext; /// This was an attempt to pass context back to the main thread, after using an annymous task runner.
        public static StringBuilder UserInput = new StringBuilder();

        // preprocessor /?/??/? - eh fuck it, it will have to be target built if you include fucking kernel32 references.

        //Modules - Only loading global most likely used.
        /*
         * Buffer -> input=modules -> Pause Main Buffer -> Enter Module While() {} for configuring modules is what I think this was for.
         * TODO: Figure out a better way to configure modules & settings .json without stopping Main. - Though I don't think there is an easy solution, I don't think exec will work across all platforms, and without loading a large ass library to handle a GUI
         */
        public static bool MODULE_CONFIGURATION = false; //will be used to pause buffer output while configuring modules, should I just make a GUI in VB to track their IP address? (I WILL BRING BACK DEAD MEMES)
        public static string? DISCORD_URI = null; // I thinks I can implements this now.


        #region MainLoopMultiThreading_TODO
        //TODO: Refactor this entire fucking piece of shit
        public class MainThread
        {
            public readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);
            public CancellationTokenSource isExiting;
            public CancellationToken cancellationToken;
            private bool _running { get; set; } = false;
            private int _tick { get; set; } = 0;
            public bool Running => isExiting.IsCancellationRequested;
            public void Start() { cancellationToken = isExiting.Token; /* Unsure if this will reset state. */ _ = Loop(); }
            public void Stop() => isExiting.Cancel();



            //public static Dictionary<string, long> FiveMillionTimers = new Dictionary<string, long>();
            //Timer mytimer = new Timer(myTimeTicker);

            public MainThread()
            {
                isExiting = new CancellationTokenSource();
                cancellationToken = isExiting.Token;

                //MainLoop.Start();
                //Remember to Thread.join() Jackass. 2024
            }

            private async Task Loop()
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                while (!isExiting.IsCancellationRequested)
                {
                    /*_tick++;

                    if(_tick >= 1_000) {
                        Console.WriteLine("Hello, This is tick");
                        _tick = 0;
                    } */

                    if (stopwatch.ElapsedMilliseconds >= 3500)
                    {
                        //Console.WriteLine("Hello, This is tick");
                        //Print("", $"How annoying? {IS_WIN}", PrintSeverity.Normal);
                        stopwatch.Restart();
                    }

                    await Task.Delay(1); // Remember we are GOTO, jumping, is why this fails first test.
                }
            }
        }
        // End MAIN_LOOP;
        #endregion

        private static OAuthClient oAuth;
        private static MainThread MainLoop = new MainThread();

        private enum userInputs
        {
            say = 2, //say, msg
            whisper = 3, // whisper, user, msg
            mute = 3, // No idea why mute was 3 args it should be 2
            test = 3, // NO idea what I was using this for.
            cat = 2 //vNyan websocket takes 1 additional arg (websocket payload).
        }


        private static int? GetEnumValueIfStartsWith(string input) {
            foreach (var enumName in Enum.GetNames(typeof(userInputs))) {
                if (input.StartsWith(enumName)) {
                    userInputs enumValue = (userInputs)Enum.Parse(typeof(userInputs), enumName);
                    return (int)enumValue;
                }
            }
            return null;
        }


        /// <summary>
        ///  Construct a string[] with a set length based on the command issued by userinput.
        /// </summary>
        /// <param name="input">Console.ReadLine()</param>
        /// <returns>string[] - command, params</returns>
        private static string[] user_InputParser(string input) // this is a mess redo it <--------------------------------------------------------------------------------------
        {
            string[] strings = null;

            int? enumValue = GetEnumValueIfStartsWith(input);
            if (enumValue.HasValue)
            {
                string[] tmp = new string[enumValue.Value]; // e.g. 2 for say => string[2] => [say, msg] 
                strings = input.Split(" ", enumValue.Value);
                if (strings.Length < tmp.Length)
                {
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        if (i > strings.Length - 1)
                            tmp[i] = "";
                        else
                            tmp[i] = strings[i];

                        /*if (string.IsNullOrEmpty(strings[i]))
                                tmp[i] = "";
                        else
                                tmp[i] = strings[i];*/
                    }
                    strings = new string[tmp.Length];
                    strings = tmp;
                }
            } else {
                strings = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            }

            return strings;
        }

        // Depre
        /*public static async Task SaendMessage(string action, params string[] dparams) {
            try {
                await wss.sendMessage(action, true, dparams);
            } catch (BotException) { } catch(Exception ex) {
                new BotException("Websocket", "Unhandled exception", ex);
            }
        }*/

        /// <summary>
        ///  The only external connection to send a websocket message, GL HAVE FUN YOU ARE ONLY ABLE TO SEND A MESSAGE BECAUSE OF MY FLOW
        ///  THE FUCKING ILLEST FLOW
        /// </summary>
        /// <param name="action">send_message or die</param>
        /// <param name="msg">String - TextXTASDF</param>
        /// <returns></returns>
        public static Task SendWebSocketMsg(string action, string msg="") => _ = wss.SendMessage(action, msg);

        /// <summary>
        ///  Entry point
        /// </summary>
        /// <param name="args">Maybe</param>
        /// <returns></returns>
        /// <exception cref="BotException"></exception>
        async static Task Main(string[] args)
        {
            //AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
            //Console.CancelKeyPress += ConsoleOnCancelKeyPress;
            //mainThreadContext = SynchronizationContext.Current;



            if (File.Exists(".env")) ENVIRONMENT_PATH = ".env";
            else {
                Print("Environment", $"Could not find the environment file, please specify the path below. Default: .env", PrintSeverity.Warn);
                while(true) {
                    Console.Write("path>");
                    var path = Console.ReadLine();

                    if (path == "exit" || path == "close" || path == "end") Environment.Exit(0);
                    if (File.Exists(path)) { ENVIRONMENT_PATH = path; break; }
                    else Print("Environment", $"Could not find the path: {path}. Please make sure the file path is correct.", PrintSeverity.Error);
                }
            }

            try {
                envManager.load();
            } catch {
                // Unrecoverable exception occured (thrown from load) need to exit regardless of BotException or General Exception.
                Print(name, $"Can't recover from error, application needs to close.{Environment.NewLine}Press any key to close.", PrintSeverity.Normal);
                Console.ReadKey();
                return;
            }

            // All Globals have been loaded.
            _ = JWT.Token(); // Store JSON Web Token Payload values for retrieval from other parts of things doing things and things.

            wss = new WebsocketClient(JWT.GetChannelIdentifier ?? string.Empty); // Set the Channel ID so interaction is possible.

            oAuth = new OAuthClient(
                HOST!,
                CLIENT_ID!,
                CLIENT_SECRET!,
                "/api/oauth/authorize",
                "/api/oauth/token",
                $"https://127.0.0.1:{LoopbackPort}/auth",
                "bot"
            );

            Console.Title = "♥ Shimamura :: 0 ♥";

            if (!MainLoop.Running) MainLoop.Start();
            //Console.Clear();
            #region Welcome ASCII garbage
            Console.ForegroundColor = ConsoleColor.Cyan;
            string headerBorder = new string('=', Console.WindowWidth);
            Console.WriteLine(headerBorder);
            Console.SetCursorPosition(0, 1);
            Console.Write($"===");
            Console.SetCursorPosition(Console.WindowWidth / 2 - 27, 1);
            Console.Write($"♥ Shimamura Bot ♥ v{Assembly.GetExecutingAssembly().GetName().Version}. Welcome !");
            Console.SetCursorPosition(Console.WindowWidth - 3, 1);
            Console.Write("===");
            Console.SetCursorPosition(0, 2);
            Console.WriteLine(headerBorder);
            Console.SetCursorPosition(1, 4);
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine("Type \"Help\" for commands, or \"Start\" to start the bot");
            #endregion

            Print(name, $"Successfully loaded environment file.", PrintSeverity.Normal);
            DEBUGGING_ENABLED = true;
            while (true) {
                /*var key = Console.ReadKey(intercept: true); //--## A fool's dream. I might come back and try to fix the buffer, until then. I need all components to work.

                if (key.Key == ConsoleKey.Enter) {*/
                    string? input = Console.ReadLine();
                    //string input = UserInput.ToString();
                    //UserInput.Clear();
                    string[] msg = input.Split(' ', 2);
                    //string[] msg = user_InputParser(input);

                    switch (msg[0].ToLower()) {
                        case "aa":
                            Print(name, $"Assert {msg}", PrintSeverity.Debug);
                            break;
                            Print(name, $"2 opt(iat exp) {JWT.GetIssuedTime ?? -1} :: {JWT.GetExpiration ?? -1}", PrintSeverity.Debug);
                            bool parse = await JWT.Token();
                            if (parse) {
                                int? issued = JWT.GetIssuedTime;
                                if (issued != null)
                                    issued = (int)issued;
                                // Everything needed for connection should be held.
                                Print(name, $"aa2 opt(iat exp) {JWT.GetIssuedTime ?? -1} '{issued}' :: {JWT.GetExpiration}", PrintSeverity.Debug);
                            } else {
                                Print(name, $"ZERO", PrintSeverity.Error);
                            }
                            break;
                        /* CURRENT WORK =================================================================================== */
                        case "revoke":
                            Print(name, $"This will revoke OAuth2 code and Web Token.{Environment.NewLine}Continue? (y/n) >", PrintSeverity.Warn);
                            bool t = false;
                            while (true) {
                                var b = Console.ReadLine()?.ToLower();
                                if (b == "y" || b == "yes") { t = true; break; }
                                else if (b == "n" || b == "no" || b == "q" || b == "exit") { break; }
                            } if (!t) break;


                            await RequestClosure();

                            _ = Task.Run(async () => {
                                var a = await oAuth.Revoke();
                                
                                if(a) {
                                    Print(name, $"OAuth2 flow succeded.", PrintSeverity.Debug);
                                } else {
                                    Print(name, "OAuth2 flow failed.", PrintSeverity.Debug);
                                }

                                Print(name, "Exiting OAuth2 anonymous Task.", PrintSeverity.Debug);
                            });
                            break;
                        case "set":
                            string[] parAGRAPHS = msg[1].Split(" ", 2);
                            switch (parAGRAPHS[0]) {
                                case "title":
                                    try {
                                        using (Joystick.API streamSettings = new()) {
                                            _ = streamSettings.SetTitleAsync(parAGRAPHS[1]);
                                            Print(name, $"Setting Stream Title: {parAGRAPHS[1]}.", PrintSeverity.Normal);
                                        }
                                    }
                                    catch (BotException) { /* no recursion */ }
                                    catch (Exception ex) { new BotException(name, $"Exception happened while setting title.", ex); }
                                    break;
                                case "bannedwords":
                                    throw new NotImplementedException();
                                    //implement.
                                    break;
                                case "greeting":
                                    throw new NotImplementedException();
                                    break;
                            }
                            break;
                        case "test":

                        if (_Twitch == null) {
                            _Twitch = new();

                            var asdf = await _Twitch.ConnectAsync();
                            if (asdf) Print(name, $"Connected to Twitch IRC.", PrintSeverity.Normal); else Print(name, $"Did not connect to Twitch IRC.", PrintSeverity.Normal);
                        } else {
                            var asdf = await _Twitch.CloseAsync();
                            if(asdf) {
                                Print(name, $"Socket closed to Twitch IRC.", PrintSeverity.Normal);
                            } else {
                                Print(name, $"Socket.. Not? closed to Twitch IRC", PrintSeverity.Normal);
                            }
                        }
                        break;
                        using (Joystick.API neverevereveverveverveer = new()) {
                            //neverevereveverveverveer.UpdateStreamSettingsAsync("Meow", "Meow, meow [meow]: \\Meow/;}{{}><.!@#$@%$^&*()_", new string[] { "tacos" });
                            var blah = await neverevereveverveverveer.GetStreamSettingsAsync();
                            //neverevereveverveverveer.Dispose();

                            Print("Debug",
                                    $"Username: {blah.username}\n" +
                                    $"Stream Title: {blah.stream_title}\n" +
                                    $"Chat Welcome Message: {blah.chat_welcome_message}\n" +
                                    $"Banned Chat Words: [{string.Join(", ", blah.banned_chat_words)}]\n" +
                                    $"Device Active: {blah.device_active}\n" +
                                    $"Photo URL: {blah.photo_url}\n" +
                                    $"Live: {blah.live}\n" +
                                    $"Number of Followers: {blah.number_of_followers}", PrintSeverity.Debug
                                );
                        }
                            /*for(int i = 0; i < 20; i++) // This is a test for prize module.
                            {
                                var player = Enum.GetNames(typeof(RandomNames))[new Random().Next(Enum.GetNames(typeof(RandomNames)).Length)];
                                var prizer = Enum.GetNames(typeof(RandomPrizes))[new Random().Next(Enum.GetNames(typeof(RandomPrizes)).Length)];
                                //_ = UpdateRewards(player, prizer, 1);
                                _ = Redeemer(player, prizer);
                            }*/
                            //_ = Modules.ModuleLoader.LoadSettings();
                            ///Print($"{DISCORD_URI}", 0);
                            break;
                            Modules.DiscordWebhook webhookd = new Modules.DiscordWebhook(DISCORD_URI, "♥ Coding/Cyberpunk ♥", "I'm coding, and then playing Cyberpunk This is a test message n shit \n new line test \r\n linefeed + carriage return test");
                            _ = webhookd.SendDiscordWebHook();
                            Print(name, $"Dones {msg[1]}", PrintSeverity.Debug);
                            break;
                        case "overunder":
                            //start a new game of over/under
                            Modules.OverUnder Game = new Modules.OverUnder("tits");
                            break;
                        case "whisper":
                            _ = Task.Run(() => {
                                string[] _tmp = msg[1].Split(" ", 2);
                                string user = _tmp[0];
                                string message = _tmp[1];

                                _ = wss.SendWhisper("send_whisper", message, user);
                            });
                            //_ = SendMessage("send_whisper", new string[] { msg[2], msg[1], "" }); // Keep until you verify it works properly.
                            break;
                        case "say":
                            _ = Task.Run(() => {
                                _ = wss.SendMessage("send_message", msg[1]);
                            });

                            //_ = SendMessage("send_message", new string[] { msg[1], "", "" });
                            break;
                        case "rage" or "eyes":
                            _ = Redeemer("adachi91", msg[0], true);
                            break;
                        case "tits":
                            _ = Redeemer("", "tits", true, 10, true);
                            break;
                        case "cumdump" or "cum" or "trip":
                            try
                            { // are you fucking happy this is what happens when you find a stranger in the alps. Because you try to convert then forget the arguments.
                                _ = Redeemer("adachi91", msg[0], true, Convert.ToInt32(msg[1]), true);
                            }
                            catch (Exception ex) { new BotException("YOU", "Hey dipshit, you forgot how to count.", ex); }
                            break;
                        case "mute":
                            //int msgid;
                            //string[] mutemsg;
                            //string mutemsgid;
                            try {
                                throw new NotImplementedException();
                                //msgid = int.Parse(tits[2]);
                            } catch (Exception e) {
                                new BotException("System", $"Could not parse to int", e);
                            }
                            //mutemsg = wss.getMessage(msgid);
                            //_ = wss.sendMessage("mute_user", new string[] { "", mutemsg[0], mutemsg[1] });
                            break;
                        case "exit" or "quit":
                            if (wss.Open)
                                await wss.Close();
                            return;
                        case "start" or "run":
                            if (JWT.Valid && !JWT.Expired) {
                                if (DEBUGGING_ENABLED) Print(name, $"User-input 'run' received, attempting to start bot.", PrintSeverity.Debug);
                                wss = new WebsocketClient(JWT.GetChannelIdentifier);
                                _ = wss.Connect();
                            } else if (JWT.Valid && JWT.Expired && !string.IsNullOrEmpty(REFRESH_TOKEN)) { // This has the potential to throw.
                                _ = Task.Run(async () => {
                                    Print(name, $"Token expired {GetUnixTimestamp() - (JWT.GetExpiration ?? -1)} seconds ago, attempting to refresh.", PrintSeverity.Normal);

                                    var err = await oAuth.RefreshToken();

                                    if (err) {
                                        await JWT.Token();

                                        Print(name, $"Token was successfully refreshed. New expiration time: {(JWT.GetExpiration ?? -1) - GetUnixTimestamp()} seconds. ☺ ♥ (^o^  )\\", PrintSeverity.Debug);
                                        if (JWT.Valid && !JWT.Expired) {
                                            // Delegate _ = wss.Connect(); back to the main thread
                                            //mainThreadContext?.Send(_ => wss.Connect().GetAwaiter().GetResult(), null); //GPT read on what is happening.
                                            Print(name, $"Token was renewed, please type 'run' again to start.", PrintSeverity.Normal);
                                        }
                                    }
                                });
                            } else {
                            _ = Task.Run(async () => {
                                bool success = await oAuth.StartFlowAsync();

                            });
                                /*_ = Task.Start(async () => {
                                    HTTPServer tempWebserver = new HTTPServer(oAuth); //TODO: make sure it has a proper timeout since it's no longer interactable.
                                    bool _oauthComplete = await tempWebserver.Start();
                                    tempWebserver = null;

                                    if (_oauthComplete) {
                                        _ = await JWT.Token();
                                        wss.SetChannelId(JWT.GetChannelIdentifier);

                                        //_ = wss.Connect();
                                        Print(name, $"Authorization complete. Type 'run' again to start the bot.", PrintSeverity.Normal);
                                    } else {
                                        Print(name, $"Unable to connect", PrintSeverity.Error);
                                    }
                                });*/
                            }
                            break;
                        case "stop":
                            if (wss.Open) {
                                Print(name, $"Stopping bot...", PrintSeverity.Normal);
                                try {
                                    _ = wss.Close();
                                }
                                catch (BotException) { }
                                catch (Exception e) { new BotException("Websocket", "Unhandled exception", e); }
                            }
                            else
                                Print(name, $"The bot is not currently active.", PrintSeverity.Warn);
                            break;
                        case "exp":
                            if (JWT.Valid && !JWT.Expired) //OAuthClient.checkJWT())
                                Print(name, $"Token expires in {(JWT.GetExpiration ?? -1) - GetUnixTimestamp()} seconds", PrintSeverity.Debug);
                            else
                                Print(name, $"Token is either invalid or expired.", PrintSeverity.Debug);
                            break;
                        case "resetenv":
                            Print(name, $"WARNING This will reset all values in your .env file. Are you sure you wish to proceed? (Y/N)", PrintSeverity.Warn);
                            while (true) {
                                var _confirm = Console.ReadLine().ToLower();

                                if (_confirm == "y" || _confirm == "yes") { FlushToDisk(true); Print(name, $".env has been reset to defaults.", PrintSeverity.Normal); break; }
                                else if (_confirm == "n" || _confirm == "no") { Print(name, $"Cancelled reset.", PrintSeverity.Normal); break; }
                                else break;
                            }
                            break;
                        case "logging":
                            LOGGING_ENABLED = !LOGGING_ENABLED;
                            _ = Task.Run(() => { envManager.FlushToDisk(); });
                            if (LOGGING_ENABLED) Print(name, $"Logging is now enabled.", PrintSeverity.Normal); else Print(name, $"Logging is now disabled.", PrintSeverity.Normal);
                            break;
                        case "help":
                            Print("Help", "Command list", PrintSeverity.Normal);
                            Print("", $"start - Starts the bot", PrintSeverity.Normal);
                            Print("", $"stop - Stops the bot", PrintSeverity.Normal);
                            Print("", $"exp - Shows how many seconds are left until your token expires", PrintSeverity.Normal);
                            Print("", $"logging - Toggle logging on/off", PrintSeverity.Normal);
                            Print("", $"resetenv - Resets the .env file to all default values (you will have to fill in the file again).", PrintSeverity.Normal);
                            Print("", $"exit or quit - Exits the program gracefully", PrintSeverity.Normal);
                            Print("", $"say - Usage: say <msg>", PrintSeverity.Normal);
                            Print("", $"whisper - Usage: <Username> <Message>", PrintSeverity.Normal);

                            Print("", $"===== Modules ====", PrintSeverity.Normal);

                            Print("", $"overunder - not finished", PrintSeverity.Normal);
                            Print("", $"rage - vibrate?", PrintSeverity.Normal);
                            Print("", $"eyes - smol eyes", PrintSeverity.Normal);
                            Print("", $"tits - expose breasts", PrintSeverity.Normal);
                            Print("", $"trip - trippy", PrintSeverity.Normal);
                            Print("", $"cum  - liquid overhead", PrintSeverity.Normal);
                            break;
                        default:
                            Print(name, "Invalid command - use help for list of commands", PrintSeverity.Normal);
                            break;
                    }
                /*} else if(key.Key == ConsoleKey.Backspace) {
                    if(UserInput.Length>=0) UserInput.Remove(UserInput.Length -1, 1);
                    //if(Console.ReadLine() != $">{UserInput.ToString()}")
                    Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write($">{UserInput.ToString()}");
                } else {
                    UserInput.Append(key.Key);
                    //if (Console.ReadLine() != $">{UserInput.ToString()}")
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write($">{UserInput.ToString()}");
                }*/
            }
        }


        private static async Task<bool> RequestClosure()
        {
            // Close WebSocket
            if (wss != null && wss.Open) await wss.Close();

            // Make sure there is no lingering API
            //To be implemented, LOL YOU CANT EVEN GET IT FUCKING WORKING

            //
            return true;
        }


        public static async Task<bool> onload_Token(string? Token, string RefreshToken="")
        {
            if(!string.IsNullOrEmpty(Token))
            {
                Console.WriteLine($"Received Tokenstring {Token.Substring(1, 3)}");
                IS_WIN = false;
                return false;
            }
            ACCESS_TOKEN = Token ?? string.Empty;
            REFRESH_TOKEN = RefreshToken;
            var _success = await JWT.Token();

            return _success;
        }


        // this whole fucking thing is pointless and a waste of time, gj.
        class UserInputHelper
        {
            string _cmd { get; set; }
            List<string> _input { get; set; }
            
            public class Parameters {
                public string Message { get; set; } // Whisper, Chat Messsage
                public string Message_ID { get; set; } // Mute, Block
                public string Username { get; set; } // Whisper, Unmute
                public string Module { get; set; } // vNyan => redeemer class / ?
                public string Module_Message { get; set; } // Assuming websocket or something I have no idea
            }

            public UserInputHelper(string whatevertheusertyped) // as of right now only 1 input takes an additional arg +2
            {
                _input = new List<string>(whatevertheusertyped.Split(' ')); //[say ]abcdefg hijklmno pqrstuv wxyz
                _cmd = _input[0];
                _input.RemoveAt(0);
                Parameters Params = new Parameters();

                switch(_cmd) {
                    case "whisper":
                        Params.Username = _input[0];
                        Params.Message = string.Join(" ", _input.GetRange(1, _input.Count - 1));
                        break;
                    default:

                        break;
                }

                //options: either switch it or create invididual methods
            }

            public void getsendwhisper(List<string> input) {
                string message = string.Join(" ", input.GetRange(1, input.Count - 1));
            }
        }

        //de..precated? I think I shutdown manually but this is a good idea.
        private static async Task<bool> WaitForClosures()
        {//to even start I need to have a pool of resources to check list down to make sure are closed and disposed or at least gracefully closed.
            await Task.Delay(100);
            throw new NotImplementedException();
            //return true;
        }


        /// <summary>
        /// Call on every type of termination to make sure that sockets, servers, etc are all shut down properly.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClose(object sender, EventArgs e)
        {

        }

        private static async void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            await Task.Delay(100);
            Console.WriteLine("Exiting process...");

            //var stopped = await Task.Start(() => WaitForClosure());
            //ExitEvent.Set();
            MainLoop.ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Console.WriteLine("Canceling process...");
            e.Cancel = true;
            MainLoop.ExitEvent.Set();
        }
    }
}
