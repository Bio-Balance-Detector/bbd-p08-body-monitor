using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Signal generator configuration options
    /// </summary>
    public class SignalGeneratorOptions
    {
        public bool Enabled { get; set; } = false;
        /// <summary>
        /// Signal generation channel (W1 or W2)
        /// </summary>
        [Obsolete]
        public byte Channel { get; set; } = 0;
        /// <summary>
        /// Native frequency of the signal generator on all channels
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Frequency { get; set; } = 1000.0f;
        /// <summary>
        /// Peak amplitude of the generated signal
        /// </summary>
        [Obsolete]
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Voltage { get; set; } = 1.0f;
        /// <summary>
        /// The repetition period of the timer that is responsible for the calculation of the current amplitudes and freqeuncies for each output. 
        /// When it's called all outputs get a new amplitude and frequency value based on the current time and the signal definition.
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public float Resolution { get; set; } = 100;
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