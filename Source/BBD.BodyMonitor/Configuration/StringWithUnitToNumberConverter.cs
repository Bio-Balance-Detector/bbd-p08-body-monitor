using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Collections.Generic; // Added for Dictionary
using System; // Added for Math.Pow and ArgumentException

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Converts a string representation of a number with an optional unit postfix (e.g., "10k", "2M", "500m", "1GB") to a numeric type.
    /// Supports standard SI unit postfixes (p, n, u, m, k, M, T, P) and binary prefixes if no space is present (e.g. "1KB" is 1024).
    /// </summary>
    public class StringWithUnitToNumberConverter : ConfigurationConverterBase
    {
        private static readonly Dictionary<string, int> _prefixPowers = new Dictionary<string, int>(System.StringComparer.Ordinal)
        {
            {"p", -4}, {"n", -3}, {"u", -2}, {"m", -1},
            {"k", 1}, {"K", 1}, // Added K for kilo
            {"M", 2}, {"G", 3}, {"T", 4}, {"P", 5}
        };

        internal bool ValidateType(object? value, Type expected)
        {
            bool result = value == null || value.GetType() == expected;
            return result;
        }

        public override bool CanConvertTo(ITypeDescriptorContext? ctx, Type? type)
        {
            return (type == typeof(int)) || (type == typeof(long)) || (type == typeof(float)) || (type == typeof(double));
        }

        public override bool CanConvertFrom(ITypeDescriptorContext? ctx, Type? type)
        {
            return type == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext? ctx, CultureInfo? ci, object? value, Type type)
        {
            throw new NotImplementedException("Conversion from numeric type back to string with unit is not implemented.");
        }

        public override object ConvertFrom(ITypeDescriptorContext? ctx, CultureInfo? ci, object data)
        {
            if (data == null)
            {
                throw new ArgumentException("Data cannot be null.", nameof(data));
            }

            if (data is not string dataStr)
            {
                throw new ArgumentException("Data must be a string.", nameof(data));
            }
            string str = dataStr.Trim();

            if (string.IsNullOrWhiteSpace(str)) { return 0.0; }

            double numberPart = 0;
            int numericPartEnd = 0;

            for (int i = 1; i <= str.Length; i++)
            {
                string potentialNumberStr = str.Substring(0, i);
                if (double.TryParse(potentialNumberStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedValue))
                {
                    numberPart = parsedValue;
                    numericPartEnd = i;
                }
                else
                {
                    if (numericPartEnd > 0)
                    {
                        break;
                    }
                    if (potentialNumberStr.Length == 1 && (potentialNumberStr[0] == '-' || potentialNumberStr[0] == '+')) {
                        continue;
                    }
                    break;
                }
            }

            if (numericPartEnd == 0) {
                 return 0.0;
            }

            string textPart = str.Substring(numericPartEnd).Trim();
            bool applyBinaryScale = !str.Contains(" ");

            if (!string.IsNullOrEmpty(textPart))
            {
                string unitPrefix = textPart.Substring(0, 1);

                if (_prefixPowers.TryGetValue(unitPrefix, out int power))
                {
                    if (power < 0) // Small prefix: p, n, u, m
                    {
                        numberPart /= Math.Pow(1000, -power);
                    }
                    else if (power > 0) // Large prefix: k, K, M, G, T, P
                    {
                        double baseValue;
                        if (power == 1) // k or K
                        {
                            if (unitPrefix == "k") // Lowercase 'k' is always decimal (1000-based)
                            {
                                baseValue = 1000;
                            }
                            else // Uppercase 'K'
                            {
                                baseValue = applyBinaryScale ? 1024 : 1000;
                            }
                        }
                        else // M, G, T, P (power > 1)
                        {
                            baseValue = applyBinaryScale ? 1024 : 1000;
                        }
                        numberPart *= Math.Pow(baseValue, power);
                    }
                }
            }
            return numberPart;
        }
    }
}
