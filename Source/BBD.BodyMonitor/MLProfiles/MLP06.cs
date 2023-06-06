using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    public class MLP06
    {
        [LoadColumn(0, 149999)]
        [VectorType(150000)]
        public float[] Features { get; set; }

        [LoadColumn(150000)]
        public float Label { get; set; }
    }
}
