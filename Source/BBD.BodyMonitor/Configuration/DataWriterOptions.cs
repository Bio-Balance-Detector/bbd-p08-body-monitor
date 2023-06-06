using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class DataWriterOptions
    {
        public bool Enabled { get; set; } = false;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 1.0f;
        /// <summary>
        /// The limits of the 16-bit output in the WAV file, ranging from -OutputRange to +OutputRange Volts.
        /// </summary>
        public float OutputRange { get; set; } = 1.0f;
        public bool SaveAsWAV { get; set; } = false;
        public bool SingleFile { get; set; } = false;
    }
}