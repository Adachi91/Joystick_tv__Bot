using System;

namespace ShimamuraBot
{
    internal class Formatter
    {

        #region Print Functionality
        private enum Death
        {
            AllYourBaseAreBelongToUs,
            ItsNotYouItsMeNoSeriouslyIDied,
            InitiatingSelfDestructionCountdown,
            ITookAnAcornToTheKnee,
            YouShallNotPass,
            HoldMyBeer,
            HoldMyCosmo,
            GameOverMan,
            HoustonWeAreAlreadyDeadByTheTimeYouReceiveThisTransmission,
            MyShoeFlewOff,
            ThereWasASpoonButWhatItDidWasALie,
            WouldYouLikeToDevelopeAnAPPWithMe,
            WellFuck,
            SoLongAndThanksForAllTheFish,
            HeMayHaveShippedHisBedButILitterallyShitTheBed,
            SomedayIWantToBePaintedLikeOneOfYourFrenchGirls,
            MyCreatorIsObviouslyAMoronForCreatingAllTheseMessages
        }


        private static string[] formatPrint(string txt) //TODO: Run random text strings to make sure it can handle []: tagging like "Hi [where] Are you [rom you there?"
        {
            string[] holder = { "", "" };
            //Yeah I overdo shit
            int startIndex = txt.IndexOf('[');
            int endIndex = txt.IndexOf("]: ");
            string tag;

            if (startIndex != -1 && endIndex != -1 && endIndex > startIndex && startIndex == 0) {
                tag = txt.Substring(startIndex + 1, (endIndex - startIndex) - 1);
                holder[0] = $"[{tag}]";
                holder[1] = txt.Substring(endIndex + 3).Trim();
            } else {
                holder[1] = txt;
            }

            return holder;
        }


        //leaving this because I found duplicate code from debugger by seeing a lot ascii char' loaded into memory twice, and was like hmm? and searched
        //ProbabilisticWithAsciiCharSearchValues and found a reference to it and indexing and figured it might be indexOf was well. Toplol debugging to find a duplicate line of code
        //Maybe I should pay more attention to "Reference" counter at the top of methods.
        /*private static string[] formatPrint(string txt)
        {
            string[] holder = { "", "" };
            int startIndex = -1;
            int endIndex = -1;

            for (int i = 0; i < txt.Length; i++)
            {
                if (txt[i] == '[' && startIndex == -1)
                {
                    startIndex = i;
                }
                else if (txt[i] == ']' && startIndex != -1)
                {
                    endIndex = i;
                    break; // Stop iterating once the end index is found
                }
            }

            if (startIndex == 0 && endIndex > startIndex && endIndex + 2 < txt.Length && txt[endIndex + 1] == ':' && txt[endIndex + 2] == ' ')
            {
                holder[0] = txt.Substring(startIndex, endIndex - startIndex + 1);
                holder[1] = txt.Substring(endIndex + 3).Trim();
            }
            else
            {
                holder[1] = txt;
            }

            return holder;
        }*/


        /// <summary>
        /// Why? because I'm nuts, and I like lua, so fuck me, no fuck you, idk could be enjoyable. Also fuck that one mother fucker on github for saying that Vulva is a profane word, you fucking moron. What? I can go on rants inside method descriptors.
        /// </summary>
        /// <param name="text">The Message</param>
        /// <param name="level">0:Debug, 1:Normal, Warn:2, Error:3, UniversalHeatDeath:4</param>
        public static void Print(string text, int level) { //https://en.wikipedia.org/wiki/ANSI_escape_code
            ConsoleColor current = Console.ForegroundColor;
            ConsoleColor debug = ConsoleColor.Cyan;
            ConsoleColor warn = ConsoleColor.Yellow;
            ConsoleColor error = ConsoleColor.Red;
            Console.SetCursorPosition(0, Console.CursorTop); //I think I need to watch this. it might be overwriting user input;
            string[] ctx = formatPrint(text); //index 0 is Tag from which class / service. index 1 is the message.

            switch (level) {
                case 0:
                    #if DEBUG
                        Console.ForegroundColor = debug; Console.Write($" [Debug]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}{Environment.NewLine}"); 
                    #endif
                    break;
                case 1: /*ctx = formatPrint(text);*/ Console.WriteLine($" [System]{ctx[0]}: {ctx[1]}");
                    break;
                case 2: Console.ForegroundColor = warn; Console.Write($" [WARN]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}{Environment.NewLine}");
                    break;
                case 3: /*Console.Write($" \x1B[38;5;9m[ERROR]{ctx[0]}: {ctx[1]}\x1B[38;5;15m{Environment.NewLine}"); test to switch to ANSI escape, good idea? great? or horrible.. */ Console.ForegroundColor = error; Console.Write($" [ERROR]{ctx[0]}:"); Console.ForegroundColor = current; Console.Write($" {ctx[1]}{Environment.NewLine}"); _ = WriteToFileShrug("ERROR", new string[] { DateTime.UtcNow.ToString(), $"{ctx[0]}: {ctx[1]}" });
                    break;
                case 4:
                    Random rand = new Random();
                    Console.WriteLine($" [Death]{ctx[0]}: If you're seeing this then somehow, somewhere in this vast universe someone invoked the wrath of Hel▲6#╒e¢◄e↕Y8AéP╚67/Y1R\\6xx9/5Ωφb198 . . . . . {(Death)Enum.GetValues(typeof(Death)).GetValue(rand.Next(Enum.GetValues(typeof (Death)).Length))}\n{ctx[1]}");
                    break;
                default: Console.WriteLine($"I don't even want to know.");
                    break;
            }
            Console.Write(">");
        }
        #endregion


        /// <summary>
        ///  Get the current UTC Unix Timestamp
        /// </summary>
        /// <returns>(long) Timestamp</returns>
        public static long GetUnixTimestamp() {
            return (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
