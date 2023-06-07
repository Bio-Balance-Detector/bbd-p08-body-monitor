namespace BBD.BodyMonitor.Sessions.Segments
{
    public class HeartRateSegment : Segment
    {
        public float BeatsPerMinute { get; set; }
        public override string ToString()
        {
            return $"{base.ToString()}: {BeatsPerMinute} BPM at {Start}";
        }
    }
}
