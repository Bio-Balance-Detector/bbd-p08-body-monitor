namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// American Academy of Sleep Medicine
    /// The AASM Manual for the Scoring of Sleep and Associated Events
    /// https://aasm.org/clinical-resources/scoring-manual/
    /// </summary>
    public enum SleepLevel
    {
        /// <summary>
        /// Subject is awake.
        /// </summary>
        Awake = 0,
        /// <summary>
        /// Rapid Eye Movement (REM) sleep stage.
        /// </summary>
        REM = 1,
        /// <summary>
        /// Light sleep stage (e.g., N1, N2 stages).
        /// </summary>
        Light = 2,
        /// <summary>
        /// Deep sleep stage (e.g., N3 stage, slow-wave sleep).
        /// </summary>
        Deep = 3,
        /// <summary>
        /// Sleep characterized by restlessness.
        /// </summary>
        Restless = 10,
        /// <summary>
        /// General state of being asleep, specific stage might be unknown or not differentiated.
        /// </summary>
        Asleep = 11,
        /// <summary>
        /// Sleep stage is unknown.
        /// </summary>
        Unknown = -1
    };

    /// <summary>
    /// Represents a segment of data detailing a period of sleep, characterized by a specific sleep level.
    /// </summary>
    public class SleepSegment : Segment
    {
        /// <summary>
        /// Gets or sets the level or stage of sleep for this segment.
        /// </summary>
        public SleepLevel Level { get; set; }
        /// <summary>
        /// Gets the duration of the sleep segment in minutes.
        /// </summary>
        public double Duration => (End - Start).TotalMinutes;
        /// <summary>
        /// Returns a string representation of the sleep segment, including its level and duration in minutes.
        /// </summary>
        /// <returns>A string summarizing the sleep segment data.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}: {Level} for {Duration} minutes";
        }
    }
}
