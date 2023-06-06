using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    public class MLP10 : MLP25k
    {
    }
    public class MLP14 : MLP25k
    {
    }
    public class MLP15 : MLP25k
    {
    }
    public class MLP16 : MLP25k
    {
    }
    public class MLP25k
    {
        [LoadColumn(0, 24999)]
        [VectorType(25000)]
        public float[] Features { get; set; }

        [LoadColumn(25000)]
        public float Label { get; set; }
    }
}
