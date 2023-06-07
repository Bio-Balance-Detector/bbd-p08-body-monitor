using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace BBD.BodyMonitor.Configuration
{
    public class StringWithUnitToNumberConverter : ConfigurationConverterBase
    {
        internal bool ValidateType(object value, Type expected)
        {
            bool result = value == null || value.GetType() == expected;
            return result;
        }

        public override bool CanConvertTo(ITypeDescriptorContext ctx, Type type)
        {
            return (type == typeof(int)) || (type == typeof(long)) || (type == typeof(float)) || (type == typeof(double));
        }

        public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
        {
            return type == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext ctx, CultureInfo ci, object value, Type type)
        {
            throw new NotImplementedException();
        }

        public override object ConvertFrom(ITypeDescriptorContext ctx, CultureInfo ci, object data)
        {
            string str = (string)data;

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
