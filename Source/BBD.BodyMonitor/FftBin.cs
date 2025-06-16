namespace BBD.BodyMonitor
{
    /// <summary>
    /// Represents a single bin in a Fast Fourier Transform (FFT) result, detailing its frequency range and properties.
    /// </summary>
    public class FftBin
    {
        /// <summary>
        /// Gets the index of this bin in the FFT data array.
        /// </summary>
        public int Index { get; internal set; }
        /// <summary>
        /// Gets the starting frequency of this bin in Hz.
        /// </summary>
        public float StartFrequency { get; internal set; }
        /// <summary>
        /// Gets the middle frequency of this bin in Hz.
        /// </summary>
        public float MiddleFrequency { get; internal set; }
        /// <summary>
        /// Gets the ending frequency of this bin in Hz.
        /// </summary>
        public float EndFrequency { get; internal set; }
        /// <summary>
        /// Gets the width (frequency range) of this bin in Hz.
        /// </summary>
        public float Width { get; internal set; }

        /// <summary>
        /// Returns a string representation of the FFT bin, showing middle frequency and width.
        /// </summary>
        /// <returns>A string in the format 'MiddleFrequency±Width/2 Hz'.</returns>
        public override string ToString()
        {
            return MiddleFrequency.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "±" + (Width / 2).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " Hz";
        }

        /// <summary>
        /// Returns a string representation of the FFT bin's middle frequency using the specified format.
        /// </summary>
        /// <param name="format">The numeric format string (e.g., '0.###').</param>
        /// <returns>The formatted string representation of the middle frequency.</returns>
        public string ToString(string format)
        {
            return MiddleFrequency.ToString(format, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}