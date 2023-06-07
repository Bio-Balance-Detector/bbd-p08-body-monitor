using BBD.BodyMonitor.Sessions.Segments;

namespace BBD.BodyMonitor.Sessions
{
    public class SegmentedData
    {
        public SleepSegment[]? Sleep { get; set; }
        public HeartRateSegment[]? HeartRate { get; set; }
        public BloodTestSegment[]? BloodTest { get; set; }
    }
}
