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
        ///  Returns a JSON wrapped Object from source.
        /// </summary>
        /// <param name="json">Source to <see cref="JsonDocument.Parse(string, JsonDocumentOptions)"/></param>
        /// <returns><see cref="object"/> || <see cref="Nullable"/></returns>
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
