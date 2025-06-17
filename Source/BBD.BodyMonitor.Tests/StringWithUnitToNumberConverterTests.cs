using Xunit;
using BBD.BodyMonitor.Configuration;
using System;
using System.Globalization;

namespace BBD.BodyMonitor.Tests
{
    public class StringWithUnitToNumberConverterTests
    {
        private readonly StringWithUnitToNumberConverter _converter = new StringWithUnitToNumberConverter();

        // Helper to call ConvertFrom - assuming it returns double as per implementation
        private double Convert(string input)
        {
            // The ConvertFrom method takes ITypeDescriptorContext, CultureInfo, and object.
            // We can pass null for context and culture as they are not used by the current implementation.
            return (double)_converter.ConvertFrom(null, CultureInfo.InvariantCulture, input);
        }

        [Theory]
        [InlineData("1k", 1000.0)]
        [InlineData("1K", 1024.0)] // Binary because no space
        [InlineData("1KB", 1024.0)]// Binary because no space
        [InlineData("1M", 1024.0 * 1024.0)] // Binary because no space
        [InlineData("1MB", 1024.0 * 1024.0)] // Binary because no space
        [InlineData("1G", 1024.0 * 1024.0 * 1024.0)] // Assuming G is treated as GB for binary
        [InlineData("1GB", 1024.0 * 1024.0 * 1024.0)]
        [InlineData("1m", 0.001)]
        [InlineData("1u", 0.000001)]
        [InlineData("1n", 0.000000001)]
        [InlineData("1p", 0.000000000001)]
        [InlineData("1T", 1024.0 * 1024.0 * 1024.0 * 1024.0)] // Binary
        [InlineData("1P", 1024.0 * 1024.0 * 1024.0 * 1024.0 * 1024.0)] // Binary
        [InlineData("2k", 2000.0)]
        [InlineData("2K", 2048.0)]
        [InlineData("0.5k", 500.0)]
        [InlineData("0.5K", 512.0)]
        [InlineData("1.5M", 1.5 * 1024.0 * 1024.0)]
        public void ConvertFrom_ValidInputs_NoSpace_BinaryForKMGTP_ReturnsCorrectDouble(string input, double expected)
        {
            Assert.Equal(expected, Convert(input), precision: 5); // Precision for double comparison
        }

        [Theory]
        [InlineData("1 k", 1000.0)]
        [InlineData("1 K", 1000.0)] // Decimal because of space
        [InlineData("1 KB", 1000.0)]// Decimal because of space
        [InlineData("1 M", 1000000.0)] // Decimal because of space
        [InlineData("1 MB", 1000000.0)] // Decimal because of space
        [InlineData("1 G", 1000000000.0)]
        [InlineData("1 GB", 1000000000.0)]
        [InlineData("1 m", 0.001)]
        [InlineData("1 u", 0.000001)]
        [InlineData("1 n", 0.000000001)]
        [InlineData("1 p", 0.000000000001)]
        [InlineData("1 T", 1000000000000.0)] // Decimal
        [InlineData("1 P", 1000000000000000.0)] // Decimal
        [InlineData("2.5 k", 2500.0)]
        [InlineData("0.5 M", 500000.0)]
        public void ConvertFrom_ValidInputs_WithSpace_Decimal_ReturnsCorrectDouble(string input, double expected)
        {
            Assert.Equal(expected, Convert(input), precision: 5);
        }

        [Theory]
        [InlineData("123", 123.0)]
        [InlineData("123.45", 123.45)]
        [InlineData("0", 0.0)]
        [InlineData("-10", -10.0)]
        [InlineData("-10.5", -10.5)]
        public void ConvertFrom_ValidInputs_NoUnits_ReturnsCorrectDouble(string input, double expected)
        {
            Assert.Equal(expected, Convert(input), precision: 5);
        }

        [Theory]
        [InlineData("  100  ", 100.0)]
        [InlineData("  1.5 k  ", 1500.0)] // Space implies decimal
        [InlineData("  1.5K  ", 1.5 * 1024.0)] // No space around K implies binary
        public void ConvertFrom_ValidInputs_WithExtraWhitespace_ReturnsCorrectDouble(string input, double expected)
        {
            Assert.Equal(expected, Convert(input), precision: 5);
        }

        [Fact]
        public void ConvertFrom_NullInput_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _converter.ConvertFrom(null, CultureInfo.InvariantCulture, null));
        }

        [Theory]
        [InlineData("abc", 0.0)] // No numeric part found, defaults to 0
        [InlineData("1Z", 1.0)]  // "Z" is not a recognized unit, so it's ignored
        [InlineData("1.1.1", 1.1)] // Parses "1.1" from "1.1.1"
        [InlineData("k", 0.0)] // No numeric part
        [InlineData("1 k k", 1000.0)] // "k k" is parsed as "k" unit, second "k" is ignored text
        [InlineData("", 0.0)] // Empty string, TryParse fails, numberPart = 0
        [InlineData("  ", 0.0)] // Whitespace only, TryParse fails, numberPart = 0
        public void ConvertFrom_MalformedInputs_ReturnsLenientConversion(string input, double expected)
        {
            // Test the actual lenient behavior of the current converter
            Assert.Equal(expected, Convert(input), precision: 5);
        }

        // Test for the CanConvertTo and CanConvertFrom methods
        [Fact]
        public void CanConvertTo_ReturnsTrueForNumericTypes()
        {
            Assert.True(_converter.CanConvertTo(null, typeof(int)));
            Assert.True(_converter.CanConvertTo(null, typeof(long)));
            Assert.True(_converter.CanConvertTo(null, typeof(float)));
            Assert.True(_converter.CanConvertTo(null, typeof(double)));
        }

        [Fact]
        public void CanConvertTo_ReturnsFalseForNonNumericTypes()
        {
            Assert.False(_converter.CanConvertTo(null, typeof(string)));
            Assert.False(_converter.CanConvertTo(null, typeof(object)));
            Assert.False(_converter.CanConvertTo(null, typeof(DateTime)));
        }

        [Fact]
        public void CanConvertFrom_ReturnsTrueForString()
        {
            Assert.True(_converter.CanConvertFrom(null, typeof(string)));
        }

        [Fact]
        public void CanConvertFrom_ReturnsFalseForNonString()
        {
            Assert.False(_converter.CanConvertFrom(null, typeof(int)));
            Assert.False(_converter.CanConvertFrom(null, typeof(object)));
        }

        [Fact]
        public void ConvertTo_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => _converter.ConvertTo(null, CultureInfo.InvariantCulture, 123.0, typeof(string)));
        }
    }
}