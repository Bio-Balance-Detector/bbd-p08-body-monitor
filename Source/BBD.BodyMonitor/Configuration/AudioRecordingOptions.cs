using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for audio recording.
    /// </summary>
    public class AudioRecordingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether audio recording is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Gets or sets the recording interval in seconds.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 10.0f;
        /// <summary>
        /// Gets or sets the preferred audio recording device.
        /// </summary>
        public string PreferredDevice { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the silence threshold for audio recording.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float SilenceThreshold { get; set; } = 0.0f;
    }
}