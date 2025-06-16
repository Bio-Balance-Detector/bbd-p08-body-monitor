using System.Globalization;

namespace BBD.BodyMonitor.Configuration
{
    public enum SignalFunction
    {
        Sine,
        Square,
        Triangle,
        Sawtooth
    }

    public enum PeriodicyMode
    {
        SingleValue,
        Sweep,
        PingPong
    }

    /// <summary>
    /// Signal definition. It sets the name, the type, the frequency and the voltage of the signal, but not its length.
    /// (eg. "Stimulation-A3=Square,500 Hz -> 600 Hz,500 mV <-> 900 mV")
    /// </summary>
    public class SignalDefinitionOptions
    {
        /// <summary>
        /// Name of the signal definition. It must be unique.
        /// </summary>
        public required string Name { get; set; }
        /// <summary>
        /// Function of the signal. It must be one of the signal types of the signal generating device (eg. "Sine", "Square", "Triangle", "Sawtooth").
        /// </summary>
        public required SignalFunction Function { get; set; }
        /// <summary>
        /// Frequency of the signal. It can be a single value (eg. 500 Hz), a frequency sweep range (indicated by ->, eg. 500 Hz -> 600 Hz), or a frequency ping-pong range (indicated by <->, eg. 500 Hz <-> 600 Hz).
        /// </summary>
        public required string Frequency { get; set; }
        /// <summary>
        /// Peak amplitude of the signal. It can be a single value (eg. 500 mV), a voltage sweep range (indicated by ->, eg. 500 mV -> 600 mV), or a voltage ping-pong range (indicated by <->, eg. 500 mV <-> 600 mV).
        /// </summary>
        public required string Amplitude { get; set; }

        /// <summary>
        /// Gets the starting frequency in Hz. This value is parsed from the <see cref="Frequency"/> string.
        /// </summary>
        public float FrequencyFrom { get; private set; }

        /// <summary>
        /// Gets the ending frequency in Hz (for sweep or ping-pong modes). This value is parsed from the <see cref="Frequency"/> string.
        /// For single value mode, this will be the same as <see cref="FrequencyFrom"/>.
        /// </summary>
        public float FrequencyTo { get; private set; }

        /// <summary>
        /// Gets the starting amplitude in Volts. This value is parsed from the <see cref="Amplitude"/> string.
        /// </summary>
        public float AmplitudeFrom { get; private set; }

        /// <summary>
        /// Gets the ending amplitude in Volts (for sweep or ping-pong modes). This value is parsed from the <see cref="Amplitude"/> string.
        /// For single value mode, this will be the same as <see cref="AmplitudeFrom"/>.
        /// </summary>
        public float AmplitudeTo { get; private set; }

        /// <summary>
        /// Gets the periodicity mode for the frequency (SingleValue, Sweep, or PingPong). This value is determined during parsing of the <see cref="Frequency"/> string.
        /// </summary>
        public PeriodicyMode FrequencyMode { get; private set; }

        /// <summary>
        /// Gets the periodicity mode for the amplitude (SingleValue, Sweep, or PingPong). This value is determined during parsing of the <see cref="Amplitude"/> string.
        /// </summary>
        public PeriodicyMode AmplitudeMode { get; private set; }

        /// <summary>
        /// Parses the <see cref="Frequency"/> string to populate <see cref="FrequencyFrom"/>, <see cref="FrequencyTo"/>, and <see cref="FrequencyMode"/>.
        /// </summary>
        public void ParseFrequency()
        {
            StringWithUnitToNumberConverter stringWithUnitToNumberConverter = new();

            string[] parts = Frequency.Split(new[] { "->", "<->" }, StringSplitOptions.None);
            FrequencyFrom = (float)(double)stringWithUnitToNumberConverter.ConvertFrom(parts[0]);
            if (parts.Length > 1)
            {
                FrequencyTo = (float)(double)stringWithUnitToNumberConverter.ConvertFrom(parts[1]);
                FrequencyMode = Frequency.Contains("<->") ? PeriodicyMode.PingPong : PeriodicyMode.Sweep;
            }
            else
            {
                FrequencyTo = FrequencyFrom;
                FrequencyMode = PeriodicyMode.SingleValue;
            }
        }

        /// <summary>
        /// Parses the <see cref="Amplitude"/> string to populate <see cref="AmplitudeFrom"/>, <see cref="AmplitudeTo"/>, and <see cref="AmplitudeMode"/>.
        /// </summary>
        public void ParseAmplitude()
        {
            StringWithUnitToNumberConverter stringWithUnitToNumberConverter = new();

            string[] parts = Amplitude.Split(new[] { "->", "<->" }, StringSplitOptions.None);
            AmplitudeFrom = (float)(double)stringWithUnitToNumberConverter.ConvertFrom(parts[0]);
            if (parts.Length > 1)
            {
                AmplitudeTo = (float)(double)stringWithUnitToNumberConverter.ConvertFrom(parts[1]);
                AmplitudeMode = Amplitude.Contains("<->") ? PeriodicyMode.PingPong : PeriodicyMode.Sweep;
            }
            else
            {
                AmplitudeTo = AmplitudeFrom;
                AmplitudeMode = PeriodicyMode.SingleValue;
            }
        }

        /// <summary>
        /// Returns a string representation of the signal definition in the format "Name=Function,FrequencyString,AmplitudeString".
        /// </summary>
        /// <returns>A string representation of the signal definition.</returns>
        public override string ToString()
        {
            NumberFormatInfo numberFormat = CultureInfo.InvariantCulture.NumberFormat;

            string frequencyString = FrequencyMode switch
            {
                PeriodicyMode.SingleValue => $"{FrequencyFrom.ToString(numberFormat)} Hz",
                PeriodicyMode.Sweep => $"{FrequencyFrom.ToString(numberFormat)} Hz -> {FrequencyTo.ToString(numberFormat)} Hz",
                PeriodicyMode.PingPong => $"{FrequencyFrom.ToString(numberFormat)} Hz <-> {FrequencyTo.ToString(numberFormat)} Hz",
                _ => throw new ArgumentOutOfRangeException(),
            };
            string voltageString = AmplitudeMode switch
            {
                PeriodicyMode.SingleValue => $"{AmplitudeFrom.ToString(numberFormat)} V",
                PeriodicyMode.Sweep => $"{AmplitudeFrom.ToString(numberFormat)} V -> {AmplitudeTo.ToString(numberFormat)} V",
                PeriodicyMode.PingPong => $"{AmplitudeFrom.ToString(numberFormat)} V <-> {AmplitudeTo.ToString(numberFormat)} V",
                _ => throw new ArgumentOutOfRangeException(),
            };
            return $"{Name}={Function},{frequencyString},{voltageString}";
        }
    }
}