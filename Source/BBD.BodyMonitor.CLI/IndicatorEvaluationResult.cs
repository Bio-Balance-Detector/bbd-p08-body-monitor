namespace BBD.BodyMonitor
{
    internal class IndicatorEvaluationResult
    {
        public int BlockIndex { get; set; }
        public int IndicatorIndex { get; set; }
        public string IndicatorName { get; set; }
        public string DisplayText { get; set; }
        public float Value { get; set; }
        public float PredictionScore { get; set; }
        public bool IsTruePositive { get; set; }
        public bool IsTrueNegative { get; set; }
        public bool IsFalsePositive { get; set; }
        public bool IsFalseNegative { get; set; }
    }
}