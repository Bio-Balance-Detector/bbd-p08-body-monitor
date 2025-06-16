using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for data acquisition.
    /// </summary>
    public class AcqusitionOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether data acquisition is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Gets or sets the channels to acquire data from.
        /// </summary>
        public string[] Channels { get; set; } = new string[0];
        /// <summary>
        /// Gets or sets the length of the acquisition buffer in seconds.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Buffer { get; set; } = 10.0f;
        /// <summary>
        /// Gets or sets the length of a data block in seconds.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Block { get; set; } = 0.1f;
        /// <summary>
        /// Gets or sets the number of samples per second (sample rate).
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public int Samplerate { get; set; } = 0;
    }
}