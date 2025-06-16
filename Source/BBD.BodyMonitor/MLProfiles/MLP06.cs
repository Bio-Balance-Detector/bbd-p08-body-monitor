using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    /// <summary>
    /// Represents the data structure for ML Profile 06.
    /// This profile uses a large vector of 150,000 features and a single float label.
    /// </summary>
    public class MLP06
    {
        /// <summary>
        /// Gets or sets the array of features for the machine learning model.
        /// These are loaded from columns 0 to 149,999 and are represented as a vector of 150,000 floating-point numbers.
        /// </summary>
        [LoadColumn(0, 149999)]
        [VectorType(150000)]
        public required float[] Features { get; set; }

        /// <summary>
        /// Gets or sets the label for the machine learning model.
        /// This is loaded from column 150,000 and is represented as a single floating-point number.
        /// </summary>
        [LoadColumn(150000)]
        public float Label { get; set; }
    }
}
