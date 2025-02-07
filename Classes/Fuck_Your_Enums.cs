using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes {
    internal class Fuck_Your_Enums {
        #region OAuth2 Enums
        /// <summary>
        ///  Declare the HTTP Authorization Type.
        /// </summary>
        public enum AuthTypes : int {
            /// <summary>
            /// No Auth header.
            /// </summary>
            None = 0,
            /// <summary>
            ///  HTTP Basic auth.
            /// </summary>
            Basic = 1,
            /// <summary>
            ///  I eh, yeah if you use this idk
            /// </summary>
            Digest = 2
        }

        /// <summary>
        ///  List of return OAuth2 states, non-exaustive.
        /// </summary>
        public enum OAuth2States {
            /// <summary>
            ///  Token is already valid.
            /// </summary>
            TokenValid,
            /// <summary>
            ///  New token acquired.
            /// </summary>
            TokenAcquired,
            /// <summary>
            ///  Failed to retrieve new token, this should be occupanied by a botexception saying where it failed.
            /// </summary>
            TokenAcquisitionFailed,
            /// <summary>
            ///  Failed renewal of token due to <b>Refresh Token</b> being invalid, revoked, or expired. 
            /// </summary>
            ReauthorizationRequired,
            /// <summary>
            ///  I forgot, but BotException will telll you.
            /// </summary>
            InvalidRequest,
            /// <summary>
            ///  Something went terribly wrong.
            /// </summary>
            DeserializationFailure,
            /// <summary>
            ///  Generic failure, unknown because I didn't cover all paths.
            /// </summary>
            UnknownFailure
        }
        #endregion

        #region Multi-Class Enums
        /// <summary>
        ///  Dat's a yuge beach.
        /// </summary>
        public enum PrintSeverity : short {
            /// <summary>
            ///  Only sent to buffer if debugging is enabled or debugger is attached.
            /// </summary>
            Debug = 0,
            /// <summary>
            ///  Standard message with no special styling.
            /// </summary>
            Normal = 1,
            /// <summary>
            ///  Alias for Normal.
            /// </summary>
            None = 1,
            /// <summary>
            ///  Like Normal but displayed in yellow.
            /// </summary>
            Warn = 2,
            /// <summary>
            ///  Like Normal but displayed in red; logs if enabled or a debugger is attached.
            /// </summary>
            Error = 3,
            /// <summary>
            ///  Displays only the username with minimal extra text.
            /// </summary>
            Chat = 4
        }


        /// <summary>
        ///  List of services implemenated to use.
        /// </summary>
        public enum Service : int {
            /// <summary>
            ///  Platform: Joystick.tv
            /// </summary>
            Joystick = 1,
            /// <summary>
            ///  Platform: Twitch.tv
            /// </summary>
            Twitch = 2,
            //whatever else you want to add
        }


        /// <summary>
        ///  Shimamura internal routing for Server-Sent Events, and API.
        /// </summary>
        public enum Route : int {
            /// <summary>
            ///  API Routing.
            /// </summary>
            API = 1,
            /// <summary>
            ///  OBS SSE Routing.
            /// </summary>
            OBS = 2,
            /// <summary>
            /// API Command Channel SSE Routing.
            /// </summary>
            SSE = 3
        }


        /// <summary>
        ///  Audio stuffs.
        /// </summary>
        public enum adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs : short {
            /// <summary>
            ///  Makes that system go BEEP.
            /// </summary>
            Beep = 1,           // sys BEEEEEEEEEEEEEEP
            /// <summary>
            ///  Eh, i'll get around to it.
            /// </summary>
            Message = 2,        // User Defined Message sound.
            /// <summary>
            ///  Hey look at your screen.
            /// </summary>
            HeyDumb = 3,
            CameraShutter = 4,  // camera_RoEAelf
            WarningOW = 5,
            WhisperOW = 6,

            // Cannot distribute with source / binary maybe? copyright.
            UwUDisturbed = 7,   // uwuhahahaha
            ggs = 8,            // gg
            UwU = 9,            // Random UwU clip
            UwUGawrGura = 10,    // uwu3
        }
        #endregion
    }
}
