namespace BBD.BodyMonitor
{
    public class MagnitudeStats
    {
        public float Min { get; internal set; }
        public int MinIndex { get; internal set; }
        public float Max { get; internal set; }
        public int MaxIndex { get; internal set; }
        public float Average { get; internal set; }
        public float Median { get; internal set; }
    }
}