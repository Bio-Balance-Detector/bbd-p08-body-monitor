using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class PostprocessingOptions
    {
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// The frequency of which the postprocessing occures
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 1.0f;
        /// <summary>
        /// The length of the buffer that gets postprocessed
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float DataBlock { get; set; } = 5.0f;
        /// <summary>
        /// FFT size
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public int FFTSize { get; set; } = 512;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float MagnitudeThreshold { get; set; } = 0.0f;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float ResampleFFTResolutionToHz { get; set; } = 1.0f;
        public bool SaveAsFFT { get; set; } = false;
        public bool SaveAsCompressedFFT { get; set; } = false;
        public bool SaveAsBinaryFFT { get; set; } = false;
        public SaveAsPngOptions SaveAsPNG { get; set; } = new SaveAsPngOptions();
    }
}