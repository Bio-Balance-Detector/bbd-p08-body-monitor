using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    public class CustomJsonConverterForNullableDateTimeOffset : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            return reader.TokenType == JsonTokenType.String ? (DateTimeOffset?)DateTimeOffset.Parse(reader.GetString()) : throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value.ToString("O"));
            }
        }
    }
}