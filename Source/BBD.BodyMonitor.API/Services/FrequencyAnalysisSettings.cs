namespace BBD.BodyMonitor.Services
{
    /// <summary>
    /// Holds settings for configuring and performing a frequency response analysis.
    /// This class is intended for internal use within the application services.
    /// </summary>
    internal class FrequencyAnalysisSettings
    {
        /// <summary>
        /// Gets or sets the starting frequency for the analysis sweep in Hertz (Hz).
        /// </summary>
        public float StartFrequency { get; set; }

        /// <summary>
        /// Gets or sets the ending frequency for the analysis sweep in Hertz (Hz).
        /// </summary>
        public float EndFrequency { get; set; }

        /// <summary>
        /// Gets or sets the sample rate to be used during the analysis in Hertz (Hz).
        /// </summary>
        public float Samplerate { get; internal set; }

        /// <summary>
        /// Gets or sets the size of the FFT (Fast Fourier Transform) to be used for analysis.
        /// </summary>
        public int FftSize { get; internal set; }

        /// <summary>
        /// Gets or sets the length of the data blocks in seconds to be used for analysis.
        /// </summary>
        public float BlockLength { get; internal set; }

        /// <summary>
        /// Gets or sets the frequency step or resolution for the analysis in Hertz (Hz).
        /// </summary>
        public float FrequencyStep { get; internal set; }

        /// <summary>
        /// Gets or sets the amplitude of the test signal to be used for the analysis in Volts (V).
        /// </summary>
        public float Amplitude { get; internal set; }
    }
}