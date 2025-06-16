namespace BBD.BodyMonitor
{
    /// <summary>
    /// Describes a task for evaluating a specific bio-indicator, potentially using a machine learning model.
    /// This class holds all necessary information to perform the evaluation for a data block.
    /// This class is intended for internal use within the application.
    /// </summary>
    internal class IndicatorEvaluationTaskDescriptor
    {
        /// <summary>
        /// Gets or sets the index of the data block to which this evaluation task pertains.
        /// </summary>
        public long BlockIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the indicator being evaluated.
        /// </summary>
        public int IndicatorIndex { get; set; }

        /// <summary>
        /// Gets or sets the name of the indicator being evaluated.
        /// </summary>
        public string IndicatorName { get; set; }

        /// <summary>
        /// Gets or sets the machine learning profile associated with this indicator evaluation.
        /// </summary>
        public MLProfile MLProfile { get; set; }

        /// <summary>
        /// Gets or sets a textual representation or additional data related to the indicator evaluation.
        /// The specific content may vary depending on the indicator type.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the filename of the machine learning model used for this evaluation task.
        /// </summary>
        public string MLModelFilename { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the result of the evaluation should be negated.
        /// </summary>
        public bool Negate { get; internal set; }

        /// <summary>
        /// Gets or sets a description of the indicator or the evaluation task.
        /// </summary>
        public string Description { get; internal set; }
    }
}