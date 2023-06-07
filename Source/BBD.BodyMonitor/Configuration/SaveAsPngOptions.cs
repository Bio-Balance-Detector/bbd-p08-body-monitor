using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class SaveAsPngOptions
    {
        public bool Enabled { get; set; } = false;
        public int TargetWidth { get; set; } = 1920;
        public int TargetHeight { get; set; } = 1080;
        public SaveAsPngRangeOptions RangeY { get; set; } = new SaveAsPngRangeOptions();
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public int RangeX { get; set; } = 0;
        public string MLProfile { get; set; } = string.Empty;
    }
}