namespace BBD.BodyMonitor.MLProfiles
{
    /// <summary>
    /// Represents the output or prediction result from a machine learning model, likely associated with ML Profile 09.
    /// </summary>
    public class MLP09Output
    {
        /// <summary>
        /// Gets or sets the predicted label as a string.
        /// </summary>
        public required string PredictedLabel { get; set; }
        /// <summary>
        /// Gets or sets the score associated with the prediction. This could be a probability, confidence level, or other metric depending on the model.
        /// </summary>
        public float Score { get; set; }
    }
}
