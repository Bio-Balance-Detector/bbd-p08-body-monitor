using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for the data writer.
    /// </summary>
    public class DataWriterOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether the data writer is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Gets or sets the interval for writing data in seconds.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets the limits of the 16-bit output in the WAV file, ranging from -OutputRange to +OutputRange Volts.
        /// </summary>
        public float OutputRange { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets a value indicating whether to save data as WAV files.
        /// </summary>
        public bool SaveAsWAV { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether to save all data into a single file or multiple files based on the interval.
        /// </summary>
        public bool SingleFile { get; set; } = false;
    }
}