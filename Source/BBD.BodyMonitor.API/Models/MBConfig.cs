namespace BBD.BodyMonitor
{
    public class MBConfig
    {
        public int TrainingTime { get; } = 1800;
        public Scenario Scenario { get; } = Scenario.Regression;
        //public Scenario Scenario { get; } = Scenario.Classification;
        public MBDataSource DataSource { get; }
        public MBEnvironment Environment { get; } = new MBEnvironment();
        public string Type { get; } = "TrainingConfig";
        public int Version { get; } = 2;

        public MBConfig(string filePath, string[] featureColumnNames, string[] labelColumnNames)
        {
            DataSource = new MBDataSource(filePath, featureColumnNames, labelColumnNames);
        }
    }

    public class MBDataSource
    {
        public string Type { get; } = "TabularFile";
        public int Version { get; } = 1;
        public string FilePath { get; }
        public string Delimiter { get; } = ",";
        public string DecimalMarker { get; } = ".";
        public bool HasHeader { get; } = true;
        public ColumnProperties[] ColumnProperties { get; }

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
                    ColumnDataFormat = ColumnDataFormat.Single,
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
                    ColumnDataFormat = ColumnDataFormat.Single,
                    IsCategorical = false,
                };
            }
        }
    }

    public class ColumnProperties
    {
        public string ColumnName { get; set; }
        public ColumnPurpose ColumnPurpose { get; set; }
        public ColumnDataFormat ColumnDataFormat { get; set; }
        public bool IsCategorical { get; set; } = false;
        public string Type { get; } = "Column";
        public int Version { get; } = 1;
    }

    public class MBEnvironment
    {
        public string Type { get; } = "LocalCPU";
        public int Version { get; } = 1;
    }

    public enum ColumnPurpose { Feature, Label, Ignore }

    public enum ColumnDataFormat { Single, Boolean, String }

    public enum Scenario { Classification, Regression }

}