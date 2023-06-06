using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    public class MLP05
    {
        [LoadColumn(0, 399)]
        [VectorType(400)]
        public float[] Features { get; set; }

        [LoadColumn(400)]
        public float Label { get; set; }
    }
}
