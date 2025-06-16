using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    /// <summary>
    /// Custom JSON converter for nullable <see cref="DateTime"/> objects (DateTime?).
    /// It correctly handles null JSON tokens and uses the round-trip ("O") format for serializing non-null DateTime values.
    /// </summary>
    public class CustomJsonConverterForNullableDateTime : JsonConverter<DateTime?>
    {
        /// <summary>
        /// Reads and converts the JSON to type <see cref="DateTime?"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted value, or null if the JSON token is null.</returns>
        /// <exception cref="JsonException">Thrown if the JSON token is not a string or null, or if parsing fails for a non-null string.</exception>
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                if (DateTime.TryParse(dateString, out DateTime result))
                {
                    return result;
                }
                throw new JsonException($"Could not parse string '{dateString}' to DateTime.");
            }
            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing nullable DateTime.");
        }

        /// <summary>
        /// Writes a specified nullable <see cref="DateTime"/> value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON. If null, a JSON null will be written.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                // "O" format ensures round-trip capability for DateTime.
                writer.WriteStringValue(value.Value.ToString("O"));
            }
        }
    }
}