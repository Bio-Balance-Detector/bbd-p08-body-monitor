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
            return this.MiddleFrequency.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + "±" + (this.Width / 2).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat) + " Hz";
        }

        public string ToString(string format)
        {
            return this.MiddleFrequency.ToString(format, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
        }
    }
}