using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    /// <summary>
    /// Represents the data structure for ML Profile 12.
    /// This profile uses a vector of 1,000 features and a single float label.
    /// </summary>
    public class MLP12
    {
        /// <summary>
        /// Gets or sets the array of features for the machine learning model.
        /// These are loaded from columns 0 to 999 and are represented as a vector of 1,000 floating-point numbers.
        /// </summary>
        [LoadColumn(0, 999)]
        [VectorType(1000)]
        public required float[] Features { get; set; }

        /// <summary>
        /// Gets or sets the label for the machine learning model.
        /// This is loaded from column 1,000 and is represented as a single floating-point number.
        /// </summary>
        [LoadColumn(1000)]
        public float Label { get; set; }
    }
}
