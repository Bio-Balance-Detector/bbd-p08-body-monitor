using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Sessions.Segments;

namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Represents a monitoring session, containing metadata about the session such as timing, location, subject, and the actual data collected. Inherits from DeidentifiedData.
    /// </summary>
    public class Session : DeidentifiedData
    {
        /// <summary>
        /// Gets or sets the version of the session data structure.
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// Gets or sets the start date and time of the session.
        /// </summary>
        public DateTimeOffset? StartedAt { get; set; }
        /// <summary>
        /// Gets or sets the end date and time of the session.
        /// </summary>
        public DateTimeOffset? FinishedAt { get; set; }
        /// <summary>
        /// Gets or sets the location where the session was recorded.
        /// </summary>
        public Location? Location { get; set; }
        /// <summary>
        /// Gets or sets the subject who was monitored during the session.
        /// </summary>
        public Subject? Subject { get; set; }
        /// <summary>
        /// Gets or sets an identifier for the data acquisition device used in the session.
        /// </summary>
        public string? DeviceIdentifier { get; set; }
        /// <summary>
        /// Gets or sets the container for various types of time-segmented data collected during this session.
        /// </summary>
        public SegmentedData? SegmentedData { get; set; }
        /// <summary>
        /// Gets or sets the BodyMonitor configuration options active during this session.
        /// </summary>
        public BodyMonitorOptions? Configuration { get; set; }

        /// <summary>
        /// Attempts to retrieve a float value from the session's segmented data based on a specified path string and time range. This method supports querying specific data points like sleep level, heart rate, or cholesterol from their respective segments within the given time window. If multiple data points fall within the range (e.g., for sleep or heart rate), their average is returned.
        /// </summary>
        /// <param name="path">A string path indicating the data to retrieve. Supported paths include:
        /// - 'Session.SegmentedData.Sleep.Level': Retrieves the sleep level (averaged if multiple segments in range).
        /// - 'Session.SegmentedData.HeartRate.BeatsPerMinute': Retrieves the heart rate (averaged if multiple segments in range).
        /// - 'Session.SegmentedData.BloodTest.Cholesterol': Retrieves the cholesterol value from the first blood test segment found within the time range.</param>
        /// <param name="startTime">The start of the time window for which to retrieve data.</param>
        /// <param name="endTime">The end of the time window for which to retrieve data.</param>
        /// <param name="value">When this method returns true, contains the retrieved float value; otherwise, 0.</param>
        /// <returns>True if a value was successfully retrieved for the given path and time range; otherwise, false.</returns>
        public bool TryToGetValue(string path, DateTimeOffset startTime, DateTimeOffset endTime, out float value)
        {
            value = 0;

            if (path == "Session.SegmentedData.Sleep.Level")
            {
                SleepSegment[]? segments = SegmentedData?.Sleep?.Where(s => s.End >= startTime && s.Start <= endTime).ToArray();

                if ((segments == null) || (segments.Length == 0))
                {
                    return false;
                }

                if (segments.Length == 1)
                {
                    value = (float)segments[0].Level;
                }

                if (segments.Length > 1)
                {
                    value = segments.Select(s => (float)s.Level).Average();
                }

                return true;
            }

            if (path == "Session.SegmentedData.HeartRate.BeatsPerMinute")
            {
                HeartRateSegment[]? segments = SegmentedData?.HeartRate?.Where(s => s.End >= startTime && s.Start <= endTime).ToArray();

                if ((segments == null) || (segments.Length == 0))
                {
                    return false;
                }

                if (segments.Length == 1)
                {
                    value = segments[0].BeatsPerMinute;
                }

                if (segments.Length > 1)
                {
                    value = segments.Select(s => s.BeatsPerMinute).Average();
                }

                return true;
            }

            if (path == "Session.SegmentedData.BloodTest.Cholesterol")
            {
                BloodTestSegment? segment = SegmentedData?.BloodTest?.Where(s => s.End >= startTime && s.Start <= endTime).FirstOrDefault();

                if (segment == null)
                {
                    return false;
                }

                value = (float)segment.Cholesterol;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns a string summary of the session.
        /// </summary>
        /// <returns>A string containing the session version, start time, location alias/name, subject alias/name, and an indicator if configuration is present.</returns>
        public override string ToString()
        {
            string sa = StartedAt == null ? "" : $"S:{StartedAt}";
            string l = Location == null ? "" : $"L:{Location?.Alias} '{Location?.Name}'";
            string s = Subject == null ? "" : $"S:{Subject?.Alias} '{Subject?.Name}'";
            string c = Configuration == null ? "" : "C:present";

            return $"V:{Version} {sa} {l} {s} {c}";
        }
    }
}
