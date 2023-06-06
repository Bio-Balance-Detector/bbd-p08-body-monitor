namespace BBD.BodyMonitor.Sessions
{
    public class Segment
    {
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset End { get; set; }

        public override string ToString()
        {
            double duration = (End - Start).TotalSeconds;

            if ((Start.Offset == TimeSpan.Zero) && (End.Offset == TimeSpan.Zero))
            {
                return $"Segment {Start:s} +{duration:0.00}s";
            }
            else
            {
                return $"Segment {Start:O} +{duration:0.00}s";
            }
        }
    }
}