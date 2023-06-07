namespace BBD.BodyMonitor
{
    public class FftBin
    {
        public int Index { get; internal set; }
        public float StartFrequency { get; internal set; }
        public float MiddleFrequency { get; internal set; }
        public float EndFrequency { get; internal set; }
        public float Width { get; internal set; }

        public override string ToString()
        {
            return MiddleFrequency.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "±" + (Width / 2).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " Hz";
        }

        public string ToString(string format)
        {
            return MiddleFrequency.ToString(format, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}