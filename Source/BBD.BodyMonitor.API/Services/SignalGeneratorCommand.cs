using BBD.BodyMonitor.Configuration;

namespace BBD.BodyMonitor.Services
{
    /// <summary>
    /// Defines the types of commands that can be issued to a signal generator.
    /// </summary>
    public enum SignalGeneratorCommandType
    {
        /// <summary>
        /// Command to start signal generation or apply new settings if already started.
        /// </summary>
        Start,
        /// <summary>
        /// Command to change parameters of an ongoing signal (currently handled by Start with new options).
        /// </summary>
        Change,
        /// <summary>
        /// Command to stop signal generation.
        /// </summary>
        Stop,
    }

    /// <summary>
    /// Represents a command scheduled for execution by a signal generator, including the time, type of command, and associated options.
    /// </summary>
    public class SignalGeneratorCommand
    {
        /// <summary>
        /// Gets the UTC timestamp indicating when the command should be executed.
        /// This represents the time of day, and the date is determined by the execution context.
        /// </summary>
        public TimeSpan Timestamp { get; internal set; }
        /// <summary>
        /// Gets the type of command to be executed (e.g., Start, Stop).
        /// </summary>
        public SignalGeneratorCommandType Command { get; internal set; }
        /// <summary>
        /// Gets the schedule options associated with this command, defining signal parameters like frequency, amplitude, and duration.
        /// This can be null, for instance, if the command is a simple 'Stop' without specific parameters from a schedule.
        /// </summary>
        public ScheduleOptions? Options { get; internal set; }

        /// <summary>
        /// Returns a string representation of the signal generator command, including its timestamp, command type, and options.
        /// </summary>
        /// <returns>A string formatted to display the command's details.</returns>
        public override string ToString()
        {
            return $"{Timestamp:hh\\:mm\\:ss\\.ff} {Command,-7} {Options}";
        }
    }
}