using Microsoft.ML.Data;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// Represents the input structure for an FFT (Fast Fourier Transform) based machine learning model.
    /// This class is intended for internal use within the application.
    /// </summary>
    internal class FftModelInput
    {
        /// <summary>
        /// Gets or sets the magnitude data of the FFT result.
        /// The vector type indicates the expected size of the array for the ML.NET model.
        /// </summary>
        [VectorType(19900)]
        public float[] MagnitudeData { get; set; }
    }
}
