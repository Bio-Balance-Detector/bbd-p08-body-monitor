namespace BBD.BodyMonitor
{
    /// <summary>
    /// Holds statistical information about FFT magnitude data, such as min, max, average, and median values.
    /// </summary>
    public class MagnitudeStats
    {
        /// <summary>
        /// Gets the minimum magnitude value found.
        /// </summary>
        public float Min { get; internal set; }
        /// <summary>
        /// Gets the index of the minimum magnitude value.
        /// </summary>
        public int MinIndex { get; internal set; }
        /// <summary>
        /// Gets the maximum magnitude value found.
        /// </summary>
        public float Max { get; internal set; }
        /// <summary>
        /// Gets the index of the maximum magnitude value.
        /// </summary>
        public int MaxIndex { get; internal set; }
        /// <summary>
        /// Gets the average magnitude value.
        /// </summary>
        public float Average { get; internal set; }
        /// <summary>
        /// Gets the median magnitude value.
        /// </summary>
        public float Median { get; internal set; }
    }
}