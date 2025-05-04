using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Il2CppNewtonsoft.Json;
using Decimal = Il2CppSystem.Decimal;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CustomAlbums.Utilities
{
    public static class Json
    {
        private static readonly JsonSerializerOptions DeserializeOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,

            // God this sucks
            ReadCommentHandling = JsonCommentHandling.Skip,

            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, DeserializeOptions);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return JsonSerializer.Deserialize<T>(stream, DeserializeOptions);
        }

        public static JsonArray ToJsonArray(this IEnumerable<object> list)
        {
            var array = new JsonArray();
            foreach (var item in list) array.Add(item);

            return array;
        }

        public static T Il2CppJsonDeserialize<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        /// <summary>
        ///     Fixes strange issue where getting a single as a decimal does not work.
        /// </summary>
        /// <param name="node">A <see cref="JsonNode"/></param>
        /// <returns>The parsed <see cref="decimal"/> value</returns>
        public static decimal GetValueAsDecimal(this JsonNode node)
        {
            return node.ToString().TryParseAsDecimal(out var result) ? result : 0M;
        }

        public static Decimal GetValueAsIl2CppDecimal(this JsonNode node)
        {
            return (Decimal)(float)node.GetValueAsDecimal();
        }
    }
}