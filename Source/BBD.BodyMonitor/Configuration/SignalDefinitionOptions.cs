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
        public required string Voltage { get; set; }

        private float FrequencyFrom { get; set; }

        private float FrequencyTo { get; set; }

        private float VoltageFrom { get; set; }

        private float VoltageTo { get; set; }

        private PeriodicyMode FrequencyMode { get; set; }

        private PeriodicyMode VoltageMode { get; set; }

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
                FrequencyMode = PeriodicyMode.SingleValue;
            }
        }

        public void ParseVoltage()
        {
            StringWithUnitToNumberConverter stringWithUnitToNumberConverter = new();

            string[] parts = Voltage.Split(new[] { "->", "<->" }, StringSplitOptions.None);
            VoltageFrom = (float)(double)stringWithUnitToNumberConverter.ConvertFrom(parts[0]);
            if (parts.Length > 1)
            {
                VoltageTo = (float)(double)stringWithUnitToNumberConverter.ConvertFrom(parts[1]);
                VoltageMode = Voltage.Contains("<->") ? PeriodicyMode.PingPong : PeriodicyMode.Sweep;
            }
            else
            {
                VoltageMode = PeriodicyMode.SingleValue;
            }
        }

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
            string voltageString = VoltageMode switch
            {
                PeriodicyMode.SingleValue => $"{VoltageFrom.ToString(numberFormat)} V",
                PeriodicyMode.Sweep => $"{VoltageFrom.ToString(numberFormat)} V -> {VoltageTo.ToString(numberFormat)} V",
                PeriodicyMode.PingPong => $"{VoltageFrom.ToString(numberFormat)} V <-> {VoltageTo.ToString(numberFormat)} V",
                _ => throw new ArgumentOutOfRangeException(),
            };
            return $"{Name}={Function},{frequencyString},{voltageString}";
        }
    }
}