using Microsoft.ML.Data;

namespace BBD.BodyMonitor
{
    internal class FftModelInput
    {
        [VectorType(19900)]
        public float[] MagnitudeData { get; set; }
    }
}
