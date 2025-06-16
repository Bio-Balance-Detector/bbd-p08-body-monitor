namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// Represents a segment of data containing heart rate information.
    /// </summary>
    public class HeartRateSegment : Segment
    {
        /// <summary>
        /// Gets or sets the heart rate in beats per minute (BPM).
        /// </summary>
        public float BeatsPerMinute { get; set; }
        /// <summary>
        /// Returns a string representation of the heart rate segment, including the beats per minute and start time.
        /// </summary>
        /// <returns>A string summarizing the heart rate data.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}: {BeatsPerMinute} BPM at {Start}";
        }
    }
}
