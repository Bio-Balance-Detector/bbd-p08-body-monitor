using System.Text;

namespace BBD.BodyMonitor.Sessions.Segments
{
    /// <summary>
    /// Represents a segment of data containing readings from one or more sensors.
    /// </summary>
    public class SensorSegment : Segment
    {
        /// <summary>
        /// Gets or sets an array of sensor names. Each element corresponds to a sensor value in <see cref="SensorValues"/>.
        /// A sensor name can be null.
        /// </summary>
        public string?[] SensorNames { get; set; } = new string?[0];
        /// <summary>
        /// Gets or sets an array of sensor values. Each element corresponds to a sensor name in <see cref="SensorNames"/>.
        /// A sensor value can be null.
        /// </summary>
        public float?[] SensorValues { get; set; } = new float?[0];
        /// <summary>
        /// Returns a string representation of the sensor segment, including its base segment information and a comma-separated list of sensor name-value pairs.
        /// </summary>
        /// <returns>A string summarizing the sensor data.</returns>
        public override string ToString()
        {
            List<string> values = new();
            StringBuilder sb = new();
            _ = sb.Append($"{base.ToString()}: ");
            for (int i = 0; i < SensorNames.Length && i < SensorValues.Length; i++)
            {
                if (SensorNames[i] == null && SensorValues[i] == null)
                {
                    continue;
                }

                values.Add($"{SensorNames[i]}={SensorValues[i]?.ToString("0.00")}");
            }
            _ = sb.Append(string.Join(",", values));

            return sb.ToString();
        }
    }
}
