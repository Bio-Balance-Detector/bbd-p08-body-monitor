using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    public class MLP12
    {
        [LoadColumn(0, 999)]
        [VectorType(1000)]
        public required float[] Features { get; set; }

        [LoadColumn(1000)]
        public float Label { get; set; }
    }
}
