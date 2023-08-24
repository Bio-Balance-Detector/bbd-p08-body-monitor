namespace BBD.BodyMonitor.Models
{
    internal class IndicatorEvaluationResult
    {
        public long BlockIndex { get; set; }
        public int IndicatorIndex { get; set; }
        public required string IndicatorName { get; set; }
        public required string DisplayText { get; set; }
        public float Value { get; set; }
        public float PredictionScore { get; set; }
        public bool IsTruePositive { get; set; }
        public bool IsTrueNegative { get; set; }
        public bool IsFalsePositive { get; set; }
        public bool IsFalseNegative { get; set; }

        public override string ToString()
        {
            return $"{DisplayText} {PredictionScore,7:+0.00;-0.00; 0.00}";
        }
    }
}