using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class AcqusitionOptions
    {
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Channels to acquire data from
        /// </summary>
        public int[] Channels { get; set; } = new int[0];
        /// <summary>
        /// The length of the buffer in seconds
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Buffer { get; set; } = 10.0f;
        /// <summary>
        /// The length of a block in seconds
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Block { get; set; } = 0.1f;
        /// <summary>
        /// Number of samples per second
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public int Samplerate { get; set; } = 0;
    }
}