namespace BBD.BodyMonitor
{
    public class MLProfile
    {
        public required string Name { get; set; }
        public float FrequencyStep { get; set; }
        public float MinFrequency { get; set; }
        public float MaxFrequency { get; set; }

        public override string ToString()
        {
            return $"ML Profile '{Name}'";
        }
    }
}
