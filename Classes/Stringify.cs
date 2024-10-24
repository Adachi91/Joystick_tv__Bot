using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace ShimamuraBot.Classes
{
    public static class JsonExtension
    {
        public static string Stringify<T>(this T obj)
        {
            return JsonSerializer.Serialize(obj);
        }
    }
}
