namespace BBD.BodyMonitor
{
    /// <summary>
    /// Configuration class for a machine learning model builder task.
    /// It defines parameters for training, data source, and environment.
    /// </summary>
    public class MBConfig
    {
        /// <summary>
        /// Gets the maximum training time in seconds for the model builder.
        /// Default is 1800 seconds (30 minutes).
        /// </summary>
        public int TrainingTime { get; } = 1800;

        /// <summary>
        /// Gets the machine learning scenario (e.g., Regression, Classification).
        /// Default is Regression.
        /// </summary>
        public Scenario Scenario { get; } = Scenario.Regression;
        //public Scenario Scenario { get; } = Scenario.Classification;

        /// <summary>
        /// Gets the data source configuration for the model builder.
        /// </summary>
        public MBDataSource DataSource { get; }

        /// <summary>
        /// Gets the environment configuration for the model builder.
        /// Default is a new <see cref="MBEnvironment"/> instance (LocalCPU).
        /// </summary>
        public MBEnvironment Environment { get; } = new MBEnvironment();

        /// <summary>
        /// Gets the type of this configuration object.
        /// Default is "TrainingConfig".
        /// </summary>
        public string Type { get; } = "TrainingConfig";

        /// <summary>
        /// Gets the version of this configuration object.
        /// Default is 2.
        /// </summary>
        public int Version { get; } = 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="MBConfig"/> class.
        /// </summary>
        /// <param name="filePath">The path to the data file for training.</param>
        /// <param name="featureColumnNames">An array of column names to be used as features.</param>
        /// <param name="labelColumnNames">An array of column names to be used as labels.</param>
        public MBConfig(string filePath, string[] featureColumnNames, string[] labelColumnNames)
        {
            DataSource = new MBDataSource(filePath, featureColumnNames, labelColumnNames);
        }
    }

    /// <summary>
    /// Defines the data source for the machine learning model builder, typically a tabular file.
    /// </summary>
    public class MBDataSource
    {
        /// <summary>
        /// Gets the type of the data source.
        /// Default is "TabularFile".
        /// </summary>
        public string Type { get; } = "TabularFile";

        /// <summary>
        /// Gets the version of this data source configuration.
        /// Default is 1.
        /// </summary>
        public int Version { get; } = 1;

        /// <summary>
        /// Gets the path to the data file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the delimiter used in the data file.
        /// Default is "," (comma).
        /// </summary>
        public string Delimiter { get; } = ",";

        /// <summary>
        /// Gets the decimal marker used in the data file.
        /// Default is "." (period).
        /// </summary>
        public string DecimalMarker { get; } = ".";

        /// <summary>
        /// Gets a value indicating whether the data file has a header row.
        /// Default is true.
        /// </summary>
        public bool HasHeader { get; } = true;

        /// <summary>
        /// Gets the properties for each column in the data source.
        /// </summary>
        public ColumnProperties[] ColumnProperties { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MBDataSource"/> class.
        /// </summary>
        /// <param name="filePath">The path to the data file.</param>
        /// <param name="featureColumnNames">An array of column names to be configured as features.</param>
        /// <param name="labelColumnNames">An array of column names to be configured as labels.</param>
        public MBDataSource(string filePath, string[] featureColumnNames, string[] labelColumnNames)
        {
            this.FilePath = filePath;

            this.ColumnProperties = new ColumnProperties[featureColumnNames.Length + labelColumnNames.Length];

            int i = 0;
            foreach (string featureColumnName in featureColumnNames)
            {
                this.ColumnProperties[i++] = new ColumnProperties()
                {
                    ColumnName = featureColumnName,
                    ColumnPurpose = ColumnPurpose.Feature,
                    ColumnDataFormat = ColumnDataFormat.Single, // Assuming features are numeric (float)
                    IsCategorical = false,
                };
            }

            foreach (string labelColumnName in labelColumnNames)
            {
                this.ColumnProperties[i++] = new ColumnProperties()
                {
                    ColumnName = labelColumnName,
                    //ColumnPurpose = labelColumnName.StartsWith("IsSubject_") ? ColumnPurpose.Label : ColumnPurpose.Ignore,
                    ColumnPurpose = ColumnPurpose.Label,
                    //ColumnDataFormat = ColumnDataFormat.Boolean,
                    ColumnDataFormat = ColumnDataFormat.Single, // Assuming labels are numeric (float) for regression/classification
                    IsCategorical = false, // Could be true for classification labels if they were strings/enums before encoding
                };
            }
        }
    }

    /// <summary>
    /// Defines the properties of a single column in the data source for model builder.
    /// </summary>
    public class ColumnProperties
    {
        /// <summary>
        /// Gets or sets the name of the column.
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the purpose of the column (e.g., Feature, Label, Ignore).
        /// </summary>
        public ColumnPurpose ColumnPurpose { get; set; }

        /// <summary>
        /// Gets or sets the data format of the column (e.g., Single, Boolean, String).
        /// </summary>
        public ColumnDataFormat ColumnDataFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the column is categorical.
        /// Default is false.
        /// </summary>
        public bool IsCategorical { get; set; } = false;

        /// <summary>
        /// Gets the type of this configuration object.
        /// Default is "Column".
        /// </summary>
        public string Type { get; } = "Column";

        /// <summary>
        /// Gets the version of this column properties configuration.
        /// Default is 1.
        /// </summary>
        public int Version { get; } = 1;
    }

    /// <summary>
    /// Defines the environment for the machine learning model builder task.
    /// </summary>
    public class MBEnvironment
    {
        /// <summary>
        /// Gets the type of the environment.
        /// Default is "LocalCPU", indicating local machine CPU will be used for training.
        /// </summary>
        public string Type { get; } = "LocalCPU";

        /// <summary>
        /// Gets the version of this environment configuration.
        /// Default is 1.
        /// </summary>
        public int Version { get; } = 1;
    }

    /// <summary>
    /// Specifies the purpose of a column in a dataset for machine learning.
    /// </summary>
    public enum ColumnPurpose
    {
        /// <summary>
        /// The column is a feature used for training the model.
        /// </summary>
        Feature,
        /// <summary>
        /// The column is a label (target variable) that the model will learn to predict.
        /// </summary>
        Label,
        /// <summary>
        /// The column should be ignored during model training.
        /// </summary>
        Ignore
    }

    /// <summary>
    /// Specifies the data format of a column.
    /// </summary>
    public enum ColumnDataFormat
    {
        /// <summary>
        /// The column contains single-precision floating-point numbers.
        /// </summary>
        Single,
        /// <summary>
        /// The column contains boolean values.
        /// </summary>
        Boolean,
        /// <summary>
        /// The column contains string values.
        /// </summary>
        String
    }

    /// <summary>
    /// Specifies the machine learning scenario or task type.
    /// </summary>
    public enum Scenario
    {
        /// <summary>
        /// The task is to predict a category or class.
        /// </summary>
        Classification,
        /// <summary>
        /// The task is to predict a continuous numerical value.
        /// </summary>
        Regression
    }
}