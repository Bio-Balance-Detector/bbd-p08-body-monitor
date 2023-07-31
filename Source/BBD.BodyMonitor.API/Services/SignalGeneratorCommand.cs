using BBD.BodyMonitor.Configuration;

namespace BBD.BodyMonitor.Services
{
    public enum SignalGeneratorCommandType
    {
        Start,
        Change,
        Stop,
    }

    public class SignalGeneratorCommand
    {
        /// <summary>
        /// UTC timestamp of the command
        /// </summary>
        public TimeSpan Timestamp { get; internal set; }
        /// <summary>
        /// Start, change or stop the signal generator
        /// </summary>
        public SignalGeneratorCommandType Command { get; internal set; }
        /// <summary>
        /// Options for the signal generator
        /// </summary>
        public ScheduleOptions? Options { get; internal set; }

        public override string ToString()
        {
            return $"{Timestamp:hh\\:mm\\:ss\\.ff} {Command,-7} {Options}";
        }
    }
}