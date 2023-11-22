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

        public static T Il2CppJsonDeserialize<T>(string text) 
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        /// <summary>
        /// Fixes strange issue where getting a single as a decimal does not work.
        /// </summary>
        /// <param name="node">A JsonNode</param>
        /// <returns>The decimal value</returns>
        public static decimal GetValueAsDecimal(this JsonNode node) =>
            decimal.TryParse(node.ToString(), out var result) ? result : 0M;

        public static Il2CppSystem.Decimal GetValueAsIl2CppDecimal(this JsonNode node) =>
            decimal.TryParse(node.ToString(), out var result) ? (Il2CppSystem.Decimal)(float)result : Il2CppSystem.Decimal.Zero;
    }
}
