namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// American Academy of Sleep Medicine
    /// The AASM Manual for the Scoring of Sleep and Associated Events
    /// https://aasm.org/clinical-resources/scoring-manual/
    /// </summary>
    public enum SleepLevel
    {
        Awake = 0,
        REM = 1,
        Light = 2,
        Deep = 3,
        Restless = 10,
        Asleep = 11,
        Unknown = -1
    };

    public class SleepSegment : Segment
    {
        public SleepLevel Level { get; set; }
        public double Duration => (End - Start).TotalMinutes;
        public override string ToString()
        {
            return $"{base.ToString()}: {Level} for {Duration} minutes";
        }
    }
}
