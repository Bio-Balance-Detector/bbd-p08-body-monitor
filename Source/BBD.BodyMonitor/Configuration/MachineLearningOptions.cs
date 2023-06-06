namespace BBD.BodyMonitor.Configuration
{
    public class MachineLearningOptions
    {
        public MLProfile[] Profiles { get; set; } = new MLProfile[0];
        public bool CSVHeaders { get; set; } = false;
        public bool GenerateMBConfigs { get; set; } = false;
    }
}