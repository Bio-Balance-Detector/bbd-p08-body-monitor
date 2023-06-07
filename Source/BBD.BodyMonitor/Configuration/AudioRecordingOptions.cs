using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class AudioRecordingOptions
    {
        public bool Enabled { get; set; } = false;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 10.0f;
        public string PreferredDevice { get; set; } = string.Empty;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float SilenceThreshold { get; set; } = 0.0f;
    }
}