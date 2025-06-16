using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    /// <summary>
    /// Custom JSON converter for nullable <see cref="DateTimeOffset"/> objects (DateTimeOffset?).
    /// It correctly handles null JSON tokens and uses the round-trip ("O") format for serializing non-null DateTimeOffset values.
    /// </summary>
    public class CustomJsonConverterForNullableDateTimeOffset : JsonConverter<DateTimeOffset?>
    {
        /// <summary>
        /// Reads and converts the JSON to type <see cref="DateTimeOffset?"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value, or null if the JSON token is null.</returns>
        /// <exception cref="JsonException">Thrown if the JSON token is not a string or null, or if parsing fails for a non-null string.</exception>
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                if (string.IsNullOrEmpty(dateString)) // Handle empty string as null, or throw if not desired
                {
                    return null;
                }
                if (DateTimeOffset.TryParse(dateString, out DateTimeOffset result))
                {
                    return result;
                }
                throw new JsonException($"Could not parse string '{dateString}' to DateTimeOffset.");
            }
            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing nullable DateTimeOffset.");
        }

        /// <summary>
        /// Writes a specified nullable <see cref="DateTimeOffset"/> value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON. If null, a JSON null will be written.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                // "O" format ensures round-trip capability for DateTimeOffset.
                writer.WriteStringValue(value.Value.ToString("O"));
            }
        }
    }
}