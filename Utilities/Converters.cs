using System.Text.Json;
using System.Text.Json.Serialization;

namespace CustomAlbums.Utilities
{
    internal class Converters
    {
        public class NumberConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
            {
                return reader.TokenType == JsonTokenType.Number ? reader.GetInt32().ToString() : reader.GetString();
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value);
            }
        }
    }
}