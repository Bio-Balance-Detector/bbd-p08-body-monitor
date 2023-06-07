using System.Text;

namespace BBD.BodyMonitor.Sessions.Segments
{
    public class SensorSegment : Segment
    {
        public string?[] SensorNames { get; set; } = new string?[0];
        public float?[] SensorValues { get; set; } = new float?[0];
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
