using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    public class MLP09
    {
        [LoadColumn(0, 29999)]
        [VectorType(30000)]
        public float[] Features { get; set; }

        [LoadColumn(30000)]
        public float Label { get; set; }
    }
}
