using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Sessions.CustomJsonConverters
{
    /// <summary>
    /// Custom JSON converter for nullable <see cref="string"/> objects (string?).
    /// It correctly handles JSON null tokens by converting them to a C# null string,
    /// and JSON string tokens to C# strings.
    /// </summary>
    public class CustomJsonConverterForNullableString : JsonConverter<string?>
    {
        /// <summary>
        /// Reads and converts the JSON to type <see cref="string?"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
        /// <returns>The converted string value, or null if the JSON token is null.</returns>
        /// <exception cref="JsonException">Thrown if the JSON token is not a string or null.</exception>
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            throw new JsonException($"Unexpected token type {reader.TokenType} when parsing nullable string.");
        }

        /// <summary>
        /// Writes a specified nullable <see cref="string"/> value as JSON.
        /// </summary>
        /// <param name="writer">The writer to write to.</param>
        /// <param name="value">The value to convert to JSON. If null, a JSON null will be written.</param>
        /// <param name="options">An object that specifies serialization options to use.</param>
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