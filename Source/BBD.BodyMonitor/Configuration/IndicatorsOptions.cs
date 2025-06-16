using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for physiological indicators.
    /// </summary>
    public class IndicatorsOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether indicator calculation is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Gets or sets the interval for calculating indicators in seconds.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets the list of machine learning models to use for indicator calculation.
        /// </summary>
        public string[] ModelsToUse { get; set; } = new string[0];
        /// <summary>
        /// Gets or sets the number of values to average for the indicators.
        /// </summary>
        public int AverageOf { get; internal set; }
    }
}