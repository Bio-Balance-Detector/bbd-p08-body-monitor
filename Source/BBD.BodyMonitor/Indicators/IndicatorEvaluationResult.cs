namespace BBD.BodyMonitor.Indicators
{
    public class IndicatorEvaluationResult
    {
        public long BlockIndex { get; set; }
        public int IndicatorIndex { get; set; }
        public required string IndicatorName { get; set; }
        public required string Text { get; set; }
        public float Value { get; set; }
        public float PredictionScore { get; set; }
        public bool IsTruePositive { get; set; }
        public bool IsTrueNegative { get; set; }
        public bool IsFalsePositive { get; set; }
        public bool IsFalseNegative { get; set; }
        public bool Negate { get; set; }
        public required string Description { get; set; }

        public override string ToString()
        {
            return $"{Text} {PredictionScore,7:+0.00;-0.00; 0.00}";
        }
    }
}