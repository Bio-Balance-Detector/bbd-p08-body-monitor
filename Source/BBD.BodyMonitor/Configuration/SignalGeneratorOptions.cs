namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Signal generator configuration options
    /// </summary>
    public class SignalGeneratorOptions
    {
        /// <summary>
        /// A series of instructions about the timing of the signals. 
        /// Its current format is {Channel ID},{time to start}/{repeat period},{signal name}({signal length}).
        /// </summary>
        public required ScheduleOptions[] Schedules { get; set; }
        /// <summary>
        /// A list of definitions of signals. Each signal has a Name, Type, Frequency and Voltage.
        /// Both Frequency and Voltage can be a single value, a frequency sweep range (indicated by ->), or a frequency ping-pong range (indicated by <->).
        /// </summary>
        public required SignalDefinitionOptions[] SignalDefinitions { get; set; }
    }
}