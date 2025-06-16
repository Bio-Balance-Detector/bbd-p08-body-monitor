using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    /// <summary>
    /// Custom JSON converter for <see cref="DateTimeOffset"/> objects.
    /// It handles null JSON tokens by returning a default <see cref="DateTimeOffset"/> instance
    /// and uses the round-trip ("O") format for serialization.
    /// </summary>
    public class CustomJsonConverterForDateTimeOffset : JsonConverter<DateTimeOffset>
    {
        /// <summary>
        /// Reads and converts the JSON to type <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value.</returns>
        /// <exception cref="JsonException">Thrown if the JSON token is not a string or null, or if parsing fails.</exception>
        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                // Handle null JSON token, return default DateTimeOffset or throw, depending on requirements.
                // Current implementation returns default(DateTimeOffset), which is January 1, 0001 00:00:00 +00:00.
                return default;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                if (DateTimeOffset.TryParse(dateString, out DateTimeOffset result))
                {
                    return result;
                }
                throw new JsonException($"Could not parse string '{dateString}' to DateTimeOffset.");
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing DateTimeOffset.");
        }

        /// <summary>
        /// Writes a specified <see cref="DateTimeOffset"/> value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        {
            // "O" format ensures round-trip capability for DateTimeOffset.
            writer.WriteStringValue(value.ToString("O"));
        }
    }
}