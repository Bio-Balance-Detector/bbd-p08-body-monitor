namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Represents a phone number with an associated type (e.g., 'Mobile', 'Home').
    /// </summary>
    public class PhoneNumber
    {
        /// <summary>
        /// Gets or sets the type of the phone number (e.g., 'Mobile', 'Work').
        /// </summary>
        public string? Type { get; set; }
        /// <summary>
        /// Gets or sets the phone number string.
        /// </summary>
        public string? Number { get; set; }
    }
}