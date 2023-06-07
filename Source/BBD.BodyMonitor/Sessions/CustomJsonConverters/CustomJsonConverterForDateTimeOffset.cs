using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    public class CustomJsonConverterForDateTimeOffset : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new DateTimeOffset();
            }

            return reader.TokenType == JsonTokenType.String ? DateTimeOffset.Parse(reader.GetString()) : throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("O"));
        }
    }
}