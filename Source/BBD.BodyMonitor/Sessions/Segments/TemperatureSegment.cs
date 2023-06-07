namespace BBD.BodyMonitor.Sessions.Segments
{
    public class TemperatureSegment : Segment
    {
        /// <summary>
        /// Temperature in degree Celsius
        /// </summary>
        public float Temperature { get; set; }
        public override string ToString()
        {
            return $"{base.ToString()}: {Temperature} ℃ at {Start}";
        }
    }
}
