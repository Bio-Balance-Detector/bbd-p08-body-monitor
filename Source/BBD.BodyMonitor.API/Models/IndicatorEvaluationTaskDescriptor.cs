namespace BBD.BodyMonitor
{
    internal class IndicatorEvaluationTaskDescriptor
    {
        public long BlockIndex { get; set; }
        public int IndicatorIndex { get; set; }
        public string IndicatorName { get; set; }
        public MLProfile MLProfile { get; set; }
        public string DisplayText { get; set; }
        public string MLModelFilename { get; internal set; }
    }
}