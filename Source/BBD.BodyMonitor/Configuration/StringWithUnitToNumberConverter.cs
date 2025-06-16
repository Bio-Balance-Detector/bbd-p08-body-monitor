using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Converts a string representation of a number with an optional unit postfix (e.g., "10k", "2M", "500m", "1GB") to a numeric type.
    /// Supports standard SI unit postfixes (p, n, u, m, k, M, T, P) and binary prefixes if no space is present (e.g. "1KB" is 1024).
    /// </summary>
    public class StringWithUnitToNumberConverter : ConfigurationConverterBase
    {
        /// <summary>
        /// Validates if the provided value is of the expected type.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <param name="expected">The expected type.</param>
        /// <returns><c>true</c> if the value is null or its type matches the expected type; otherwise, <c>false</c>.</returns>
        internal bool ValidateType(object? value, Type expected)
        {
            bool result = value == null || value.GetType() == expected;
            return result;
        }

        /// <summary>
        /// Determines whether this converter can convert an object to the specified type.
        /// </summary>
        /// <param name="ctx">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="type">A <see cref="Type"/> that represents the type you want to convert to.</param>
        /// <returns><c>true</c> if this converter can perform the conversion to int, long, float, or double; otherwise, <c>false</c>.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext? ctx, Type? type)
        {
            return (type == typeof(int)) || (type == typeof(long)) || (type == typeof(float)) || (type == typeof(double));
        }

        /// <summary>
        /// Determines whether this converter can convert an object of the given type to the type of this converter.
        /// </summary>
        /// <param name="ctx">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="type">A <see cref="Type"/> that represents the type you want to convert from.</param>
        /// <returns><c>true</c> if this converter can perform the conversion from a string; otherwise, <c>false</c>.</returns>
        public override bool CanConvertFrom(ITypeDescriptorContext? ctx, Type? type)
        {
            return type == typeof(string);
        }

        /// <summary>
        /// Converts the given value object to the specified type, using the arguments.
        /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
        /// </summary>
        /// <param name="ctx">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="ci">The <see cref="CultureInfo"/> to use as the current culture.</param>
        /// <param name="value">The <see cref="object"/> to convert.</param>
        /// <param name="type">The <see cref="Type"/> to convert the <paramref name="value"/> parameter to.</param>
        /// <returns>The converted object.</returns>
        /// <exception cref="NotImplementedException">This method is not implemented.</exception>
        public override object ConvertTo(ITypeDescriptorContext? ctx, CultureInfo? ci, object? value, Type type)
        {
            throw new NotImplementedException("Conversion from numeric type back to string with unit is not implemented.");
        }

        /// <summary>
        /// Converts the given string object to a double, using the arguments.
        /// The string can contain SI unit postfixes (e.g., "10k" for 10000, "1m" for 0.001).
        /// If the string contains a space, decimal prefixes (1000-based) are used. Otherwise, binary prefixes (1024-based) are assumed for units like 'k', 'M', etc.
        /// </summary>
        /// <param name="ctx">An <see cref="ITypeDescriptorContext"/> that provides a format context.</param>
        /// <param name="ci">The <see cref="CultureInfo"/> to use as the current culture (ignored, InvariantCulture is used for parsing).</param>
        /// <param name="data">The string data to convert. Must be a string.</param>
        /// <returns>A double representing the numeric value of the input string.</returns>
        /// <exception cref="ArgumentException">Thrown if the input data is not a string or if the number part cannot be parsed.</exception>
        public override object ConvertFrom(ITypeDescriptorContext? ctx, CultureInfo? ci, object data)
        {
            if (data is not string dataStr)
            {
                throw new ArgumentException("Data must be a string.", nameof(data));
            }
            string str = dataStr.Trim();

            string[] postfixes = { "p", "n", "u", "m", "", "k", "M", "T", "P" };
            bool binaryMode = !str.Contains(" ");

            int numberIndex = 1;
            double numberPart = 0;
            while ((numberIndex <= str.Length) && double.TryParse(str[..numberIndex], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out double parsedNumberPart))
            {
                numberIndex++;
                numberPart = parsedNumberPart;
            }

            string textPart = str[(numberIndex - 1)..].Trim();

            int postfixIndex = 4;
            for (int i = 0; i < postfixes.Length; i++)
            {
                if ((postfixes[i] != "") && textPart.StartsWith(postfixes[i]))
                {
                    postfixIndex = i;
                    break;
                }
            }

            while (postfixIndex < 4)
            {
                numberPart /= binaryMode ? 1024 : 1000;
                postfixIndex++;
            }

            while (postfixIndex > 4)
            {
                numberPart *= binaryMode ? 1024 : 1000;
                postfixIndex--;
            }

            return numberPart;
        }
    }
}
