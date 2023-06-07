namespace BBD.BodyMonitor.Sessions.Segments
{
    public class Segment
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        public override string ToString()
        {
            double duration = (End - Start).TotalSeconds;

            return Start.Offset == TimeSpan.Zero && End.Offset == TimeSpan.Zero
                ? $"Segment {Start:s} +{duration:0.00}s"
                : $"Segment {Start:O} +{duration:0.00}s";
        }
    }
}