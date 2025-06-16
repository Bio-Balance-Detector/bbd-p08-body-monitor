namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Represents the current status of a signal generator channel.
    /// </summary>
    public class SignalGeneratorStatus
    {
        /// <summary>
        /// Gets the identifier of the signal generator channel (e.g., "W1", "W2").
        /// </summary>
        public string ChannelId { get; internal set; }

        /// <summary>
        /// Gets the hardware index of the signal generator channel.
        /// </summary>
        public byte ChannelIndex { get; internal set; }

        /// <summary>
        /// Gets the raw state value of the signal generator channel, typically from the DWF library.
        /// See DWF library documentation for specific state meanings (e.g., DwfStateRunning, DwfStateDone).
        /// </summary>
        public byte State { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether the signal generator channel is currently running (outputting a signal).
        /// </summary>
        public bool IsRunning { get; internal set; }

        /// <summary>
        /// Gets the current frequency of the signal being generated, in Hertz (Hz).
        /// </summary>
        public double Frequency { get; internal set; }

        /// <summary>
        /// Gets the current amplitude of the signal being generated, in Volts (V).
        /// </summary>
        public double Amplitude { get; internal set; }
    }
}