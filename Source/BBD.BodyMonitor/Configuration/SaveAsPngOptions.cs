using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for saving data as PNG images.
    /// </summary>
    public class SaveAsPngOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether saving as PNG is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Gets or sets the target width of the PNG image in pixels.
        /// </summary>
        public int TargetWidth { get; set; } = 1920;
        /// <summary>
        /// Gets or sets the target height of the PNG image in pixels.
        /// </summary>
        public int TargetHeight { get; set; } = 1080;
        /// <summary>
        /// Gets or sets the options for the Y-axis range of the PNG image.
        /// </summary>
        public SaveAsPngRangeOptions RangeY { get; set; } = new SaveAsPngRangeOptions();
        /// <summary>
        /// Gets or sets the range of the X-axis for the PNG image, typically representing time or samples.
        /// The exact unit depends on the context (e.g., seconds, number of samples).
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public int RangeX { get; set; } = 0;
        /// <summary>
        /// Gets or sets the machine learning profile to use when generating the PNG image.
        /// </summary>
        /// <remarks>
        /// This might influence how data is visualized or which features are highlighted.
        /// </remarks>
        public string MLProfile { get; set; } = string.Empty;
    }
}