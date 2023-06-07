using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    public class CustomJsonConverterForNullableString : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.TokenType == JsonTokenType.String ? reader.GetString() : throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value);
            }
        }
    }
}