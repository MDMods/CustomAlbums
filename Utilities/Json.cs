using System.Text.Json;
using System.Text.Json.Nodes;
using JsonConvert = Il2CppNewtonsoft.Json.JsonConvert;

namespace CustomAlbums.Utilities
{
    public static class Json
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true
        };

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
        }

        public static JsonArray ToJsonArray(this IEnumerable<object> list)
        {
            var array = new JsonArray();
            foreach (var item in list)
            {
                array.Add(item);
            }

            return array;
        }

        public static T Il2CppJsonDeserialize<T>(string text) {
            return JsonConvert.DeserializeObject<T>(text);
        }
    }
}
