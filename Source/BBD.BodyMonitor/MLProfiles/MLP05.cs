using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    /// <summary>
    /// Represents the data structure for ML Profile 05.
    /// This profile appears to use a vector of 400 features and a single float label.
    /// </summary>
    public class MLP05
    {
        /// <summary>
        /// Gets or sets the array of features for the machine learning model.
        /// These are loaded from columns 0 to 399 and are represented as a vector of 400 floating-point numbers.
        /// </summary>
        [LoadColumn(0, 399)]
        [VectorType(400)]
        public required float[] Features { get; set; }

        /// <summary>
        /// Gets or sets the label for the machine learning model.
        /// This is loaded from column 400 and is represented as a single floating-point number.
        /// </summary>
        [LoadColumn(400)]
        public float Label { get; set; }
    }
}
