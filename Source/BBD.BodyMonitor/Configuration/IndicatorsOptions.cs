using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class IndicatorsOptions
    {
        public bool Enabled { get; set; } = false;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Interval { get; set; } = 1.0f;
        public string[] ModelsToUse { get; set; } = new string[0];
    }
}