namespace BBD.BodyMonitor.Services
{
    internal class FrequencyAnalysisSettings
    {
        public float StartFrequency { get; set; }
        public float EndFrequency { get; set; }
        public float Samplerate { get; internal set; }
        public int FftSize { get; internal set; }
        public float BlockLength { get; internal set; }
        public float FrequencyStep { get; internal set; }
        public float Amplitude { get; internal set; }
    }
}