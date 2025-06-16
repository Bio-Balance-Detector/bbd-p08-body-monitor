using BBD.BodyMonitor.Sessions.Segments;

namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Container for various types of time-segmented data recorded during a session.
    /// </summary>
    public class SegmentedData
    {
        /// <summary>
        /// Gets or sets an array of sleep segments.
        /// </summary>
        public SleepSegment[]? Sleep { get; set; }
        /// <summary>
        /// Gets or sets an array of heart rate segments.
        /// </summary>
        public HeartRateSegment[]? HeartRate { get; set; }
        /// <summary>
        /// Gets or sets an array of blood test segments.
        /// </summary>
        public BloodTestSegment[]? BloodTest { get; set; }
        /// <summary>
        /// Gets or sets an array of generic sensor data segments.
        /// </summary>
        public SensorSegment[]? Sensors { get; set; }
    }
}
