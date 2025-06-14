using ShimamuraBot.Web.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ShimamuraBot.Classes
{
    internal class AudioOot {
        private static string name = $"Audio";

        /// <summary>
        /// 
        /// </summary>
        /// <remarks><b>OFFSET BY 2</b> because, BEEP</remarks>
        private static readonly List<string> namenamenanmenmaenmanmemnenamnemnnmeamaenmenanmeamneanmaemnmnamneamneanmemnmaenfenjklfnkjlewnjknaknem = new() {
"whisper.mp3", //2
            "dumb.mp3",//3
                                            "cam.mp3",
                        "warning.mp3",

    "whisper.mp3",         "gg.mp3",//8
            "uwu.mp3" //10
        };

        [Obsolete("Shit's dead yo.")]
        private static Task WIN_PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs Clip) {

            switch(Clip) {
                case adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.Beep:
                    Console.Beep();
                    break;
                case adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.HeyDumb:

                    break;
                default: break;
            }

            return Task.CompletedTask;
        }

        [Obsolete("Shit's dead yo.")]
        private static Task TUX_PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs Clip) {

            switch (Clip) {
                case adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.Beep:
                    Console.Beep();
                    break;
                default: break;
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///  Play the audio clip through a webbrowser for universal interopability.
        /// </summary>
        /// <remarks><u>This will not work on most browsers</u> due to user interaction required to auto play media files.</remarks>
        /// <param name="Clip"><see cref="adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs"/> - Clip to play</param>
        /// <param name="webinterface"><see cref="WebInterface"/> - A reference to the WebUI</param>
        public static Task PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs Clip, WebInterface webinterface) {
            string path;
            switch(Clip) {
                case adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs.HeyDumb:
                    path = "./dumb.mp3";
                    break;
                default:
                    path = string.Empty;
                    break;
            }

            if(string.IsNullOrEmpty(path)) {
                new BotException(name, "Could not find clip.");
                return Task.CompletedTask; // Failed Succesfully!
            }

            if(webinterface.Open)
                webinterface.SendSSEAudioAsync(path);

            return Task.CompletedTask;
        }

        /// <summary>
        ///  Play audio clip through the console application.
        /// </summary>
        /// <remarks>You will need to either have global sound enabled, or add the application as a sound source in OBS.</remarks>
        /// <param name="Clip"><see cref="adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs"/> - Clip to play</param>
        public static Task PlayAudioAsync(adsfasdfasdfasFUCKYOUdfsdafasdfasdfasdfsadfsadfs Clip) {
            //if (DEBUGGING_ENABLED) Print(name, $"Attempting to play audio clip: {(short)Clip}", PrintSeverity.Debug);
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    WIN_PlayAudioAsync(Clip);
                } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    TUX_PlayAudioAsync(Clip);
                } else {
                    new BotException(name, "No audio system implemented for this operating system.");
                }
            } catch { }
            return Task.CompletedTask;
        }
    }
}
