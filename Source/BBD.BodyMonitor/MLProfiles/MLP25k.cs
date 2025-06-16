using Microsoft.ML.Data;

namespace BBD.BodyMonitor.MLProfiles
{
    /// <summary>
    /// Represents the data structure for ML Profile 10, which inherits its structure from <see cref="MLP25k"/>.
    /// This profile uses a vector of 25,000 features and a single float label.
    /// </summary>
    public class MLP10 : MLP25k
    {
    }
    /// <summary>
    /// Represents the data structure for ML Profile 14, which inherits its structure from <see cref="MLP25k"/>.
    /// This profile uses a vector of 25,000 features and a single float label.
    /// </summary>
    public class MLP14 : MLP25k
    {
    }
    /// <summary>
    /// Represents the data structure for ML Profile 15, which inherits its structure from <see cref="MLP25k"/>.
    /// This profile uses a vector of 25,000 features and a single float label.
    /// </summary>
    public class MLP15 : MLP25k
    {
    }
    /// <summary>
    /// Represents the data structure for ML Profile 16, which inherits its structure from <see cref="MLP25k"/>.
    /// This profile uses a vector of 25,000 features and a single float label.
    /// </summary>
    public class MLP16 : MLP25k
    {
    }
    /// <summary>
    /// Represents the base data structure for ML Profiles using a vector of 25,000 features and a single float label.
    /// Specific profiles like <see cref="MLP10"/>, <see cref="MLP14"/>, <see cref="MLP15"/>, and <see cref="MLP16"/> inherit from this class.
    /// </summary>
    public class MLP25k
    {
        /// <summary>
        /// Gets or sets the array of features for the machine learning model.
        /// These are loaded from columns 0 to 24,999 and are represented as a vector of 25,000 floating-point numbers.
        /// </summary>
        [LoadColumn(0, 24999)]
        [VectorType(25000)]
        public required float[] Features { get; set; }

        /// <summary>
        /// Gets or sets the label for the machine learning model.
        /// This is loaded from column 25,000 and is represented as a single floating-point number.
        /// </summary>
        [LoadColumn(25000)]
        public float Label { get; set; }
    }
}
