using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace ShimamuraBot.Classes
{
    public static class JsonExtension {
        private static string name = "JsonExtension";

        public static string Stringify<T>(this T obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        /// <summary>
        ///  Returns obj and stuff. My god I can't explain shit right now. I'm tired, leave me alone.
        /// </summary>
        /// <param name="json">Whatever to parse, bitch you better make sure it's JSON otherwise idk</param>
        /// <returns><see cref="object"/> || Null Nil Undefined Err Pizza oven</returns>
        public static object? Parse(this string json) {
            try {
                using JsonDocument generic = JsonDocument.Parse(json);
                return generic.RootElement.Clone();
            } catch (Exception ex) {
                new BotException($"{name}:Parse", $"Unable to parse.", ex);
                return null!;
            }
        }
    }
}
