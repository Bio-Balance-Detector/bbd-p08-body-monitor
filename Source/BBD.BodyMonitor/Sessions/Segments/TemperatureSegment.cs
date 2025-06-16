namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// Represents a segment of data containing a temperature reading.
    /// </summary>
    public class TemperatureSegment : Segment
    {
        /// <summary>
        /// Gets or sets the temperature in degrees Celsius.
        /// </summary>
        public float Temperature { get; set; }
        /// <summary>
        /// Returns a string representation of the temperature segment, including the temperature value in Celsius and the start time.
        /// </summary>
        /// <returns>A string summarizing the temperature data.</returns>
        public override string ToString()
        {
            return $"{base.ToString()}: {Temperature} ℃ at {Start}";
        }
    }
}
