namespace BBD.BodyMonitor
{
    /// <summary>
    /// Defines the parameters for a Machine Learning profile, specifying frequency range and step for FFT data processing.
    /// </summary>
    public class MLProfile
    {
        /// <summary>
        /// Gets or sets the unique name of the machine learning profile.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Gets or sets the frequency step in Hz to be used for this profile.
        /// </summary>
        public float FrequencyStep { get; set; }
        /// <summary>
        /// Gets or sets the minimum frequency in Hz to be considered for this profile.
        /// </summary>
        public float MinFrequency { get; set; }
        /// <summary>
        /// Gets or sets the maximum frequency in Hz to be considered for this profile.
        /// </summary>
        public float MaxFrequency { get; set; }

        /// <summary>
        /// Returns a string representation of the ML profile.
        /// </summary>
        /// <returns>A string in the format 'ML Profile 'Name''.</returns>
        public override string ToString()
        {
            return $"ML Profile '{Name}'";
        }
    }
}
