using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for the range (typically Y-axis) of a PNG image.
    /// </summary>
    public class SaveAsPngRangeOptions
    {
        /// <summary>
        /// Gets or sets the minimum value of the range.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Min { get; set; } = 0.0f;
        /// <summary>
        /// Gets or sets the maximum value of the range.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Max { get; set; } = 100.0f;
        /// <summary>
        /// Gets or sets the unit of measurement for the range values (e.g., "V", "mV", "dB").
        /// </summary>
        public string Unit { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the format string used for displaying the range values (e.g., "0.00", "N2").
        /// </summary>
        public string Format { get; set; } = "0.00";
    }
}