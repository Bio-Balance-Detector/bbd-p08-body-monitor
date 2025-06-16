namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// Represents a base class for a time-bound segment of data or an event within a session.
    /// </summary>
    public class Segment
    {
        /// <summary>
        /// Gets or sets the start date and time of the segment.
        /// </summary>
        public DateTimeOffset Start { get; set; }
        /// <summary>
        /// Gets or sets the end date and time of the segment.
        /// </summary>
        public DateTimeOffset End { get; set; }

        /// <summary>
        /// Returns a string representation of the segment, including its start time and duration.
        /// The start time format depends on whether the offset is zero (UTC) or not.
        /// </summary>
        /// <returns>A string summarizing the segment's time boundaries.</returns>
        public override string ToString()
        {
            double duration = (End - Start).TotalSeconds;

            // Use sortable format "s" (yyyy-MM-ddTHH:mm:ss) if UTC, otherwise use round-trip "O" format.
            return Start.Offset == TimeSpan.Zero && End.Offset == TimeSpan.Zero
                ? $"Segment {Start:s} +{duration:0.00}s"
                : $"Segment {Start:O} +{duration:0.00}s";
        }
    }
}