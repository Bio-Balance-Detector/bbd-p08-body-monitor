namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Represents identity information, including full name, email contacts, and phone numbers.
    /// </summary>
    public class Identity
    {
        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        public string? FullName { get; set; }
        /// <summary>
        /// Gets or sets an array of contact email addresses.
        /// </summary>
        public string[]? ContactEmails { get; set; }
        /// <summary>
        /// Gets or sets an array of phone numbers.
        /// </summary>
        public PhoneNumber[]? PhoneNumbers { get; set; }
    }
}
