using Microsoft.ML.Data;

namespace BBD.BodyMonitor
{
    internal class FftModelOutput
    {
        [VectorType(2)]
        public float[] Scores { get; set; }
    }
}
