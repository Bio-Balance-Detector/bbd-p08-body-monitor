namespace BBD.BodyMonitor
{
    internal class IndicatorEvaluationTaskDescriptor
    {
        public long BlockIndex { get; set; }
        public int IndicatorIndex { get; set; }
        public string IndicatorName { get; set; }
        public MLProfile MLProfile { get; set; }
        public string Text { get; set; }
        public string MLModelFilename { get; internal set; }
        public bool Negate { get; internal set; }
        public string Description { get; internal set; }
    }
}