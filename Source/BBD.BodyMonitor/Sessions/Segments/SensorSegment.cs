using System.Text;

namespace BBD.BodyMonitor.Sessions
{
    public class SensorSegment : Segment
    {
        public string?[] SensorNames { get; set; } = new string?[0];
        public float?[] SensorValues { get; set; } = new float?[0];
        public override string ToString()
        {
            List<string> values = new List<string>();
            StringBuilder sb = new StringBuilder();
            sb.Append($"{base.ToString()}: ");
            for (int i = 0; (i < SensorNames.Length) && (i < SensorValues.Length); i++)
            {
                if ((SensorNames[i] == null) && (SensorValues[i] == null))
                {
                    continue;
                }
                
                values.Add($"{SensorNames[i]}={SensorValues[i]?.ToString("0.00")}");
            }
            sb.Append(String.Join(",", values));

            return sb.ToString();
        }
    }
}
