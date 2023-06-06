using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class SignalGeneratorOptions
    {
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Signal generation channel (W1 or W2)
        /// </summary>
        public byte Channel { get; set; } = 0;
        /// <summary>
        /// Generated signal frequency
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Frequency { get; set; } = 1000.0f;
        /// <summary>
        /// Peak amplitude of the generated signal
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Voltage { get; set; } = 1.0f;
    }
}