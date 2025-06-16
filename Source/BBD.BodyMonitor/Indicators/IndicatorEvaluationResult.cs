namespace BBD.BodyMonitor.Indicators
{
    /// <summary>
    /// Represents the result of evaluating a physiological indicator.
    /// </summary>
    public class IndicatorEvaluationResult
    {
        /// <summary>
        /// Gets or sets the index of the data block for which the indicator was evaluated.
        /// </summary>
        public long BlockIndex { get; set; }
        /// <summary>
        /// Gets or sets the index of the indicator.
        /// </summary>
        public int IndicatorIndex { get; set; }
        /// <summary>
        /// Gets or sets the name of the indicator.
        /// </summary>
        public required string IndicatorName { get; set; }
        /// <summary>
        /// Gets or sets the textual representation or label of the indicator's state or result.
        /// </summary>
        public required string Text { get; set; }
        /// <summary>
        /// Gets or sets the numerical value of the indicator.
        /// </summary>
        public float Value { get; set; }
        /// <summary>
        /// Gets or sets the prediction score associated with the indicator evaluation, typically from a machine learning model.
        /// </summary>
        public float PredictionScore { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the result is a true positive.
        /// </summary>
        public bool IsTruePositive { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the result is a true negative.
        /// </summary>
        public bool IsTrueNegative { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the result is a false positive.
        /// </summary>
        public bool IsFalsePositive { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the result is a false negative.
        /// </summary>
        public bool IsFalseNegative { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the interpretation of the indicator should be negated.
        /// </summary>
        public bool Negate { get; set; }
        /// <summary>
        /// Gets or sets a description of the indicator evaluation or its outcome.
        /// </summary>
        public required string Description { get; set; }

        /// <summary>
        /// Returns a string representation of the indicator evaluation result, typically including the text and prediction score.
        /// </summary>
        /// <returns>A string in the format: "Text +X.XX" (prediction score formatted to two decimal places, with sign).</returns>
        public override string ToString()
        {
            return $"{Text} {PredictionScore,7:+0.00;-0.00; 0.00}";
        }
    }
}