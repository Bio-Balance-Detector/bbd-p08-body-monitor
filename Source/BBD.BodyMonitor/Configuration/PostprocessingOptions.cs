using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for postprocessing of acquired data.
    /// </summary>
    public class PostprocessingOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether postprocessing is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Gets or sets the interval in seconds at which postprocessing occurs.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets the length of the data block in seconds that gets postprocessed.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float DataBlock { get; set; } = 5.0f;
        /// <summary>
        /// Gets or sets the Fast Fourier Transform (FFT) size.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public int FFTSize { get; set; } = 512;
        /// <summary>
        /// Gets or sets the magnitude threshold for FFT results. Values below this threshold may be filtered out.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float MagnitudeThreshold { get; set; } = 0.0f;
        /// <summary>
        /// Gets or sets the target resolution in Hz to resample the FFT results to.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float ResampleFFTResolutionToHz { get; set; } = 1.0f;
        /// <summary>
        /// Gets or sets a value indicating whether to save the FFT results.
        /// </summary>
        public bool SaveAsFFT { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether to save the FFT results in a compressed format.
        /// </summary>
        public bool SaveAsCompressedFFT { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether to save the FFT results in a binary format.
        /// </summary>
        public bool SaveAsBinaryFFT { get; set; } = false;
        /// <summary>
        /// Gets or sets the options for saving postprocessed data as PNG images.
        /// </summary>
        public SaveAsPngOptions SaveAsPNG { get; set; } = new SaveAsPngOptions();
    }
}