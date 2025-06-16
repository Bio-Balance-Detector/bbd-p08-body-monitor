namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for ThingSpeak integration.
    /// </summary>
    public class ThingSpeakOptions
    {
        /// <summary>
        /// Gets or sets the API endpoint URI for ThingSpeak.
        /// </summary>
        /// <remarks>
        /// Defaults to "https://api.thingspeak.com".
        /// </remarks>
        public Uri APIEndpoint { get; set; } = new Uri("https://api.thingspeak.com");
        /// <summary>
        /// Gets or sets the API key for writing data to a ThingSpeak channel.
        /// </summary>
        public string? APIKey { get; set; }
    }
}