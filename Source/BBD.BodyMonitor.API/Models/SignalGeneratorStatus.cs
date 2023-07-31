namespace BBD.BodyMonitor.Models
{
    public class SignalGeneratorStatus
    {
        public string ChannelId { get; internal set; }
        public byte ChannelIndex { get; internal set; }
        public byte State { get; internal set; }
        public bool IsRunning { get; internal set; }
        public double Frequency { get; internal set; }
        public double Amplitude { get; internal set; }
    }
}