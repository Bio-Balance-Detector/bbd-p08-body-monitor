using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class SaveAsPngRangeOptions
    {
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Min { get; set; } = 0.0f;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Max { get; set; } = 100.0f;
        public string Unit { get; set; } = string.Empty;
        public string Format { get; set; } = "0.00";
    }
}