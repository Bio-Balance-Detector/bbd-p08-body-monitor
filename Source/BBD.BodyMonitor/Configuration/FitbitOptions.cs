namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for Fitbit integration.
    /// </summary>
    public class FitbitOptions
    {
        /// <summary>
        /// Gets or sets the Fitbit application Client ID.
        /// </summary>
        public string? ClientID { get; set; }
        /// <summary>
        /// Gets or sets the Fitbit application Client Secret.
        /// </summary>
        public string? ClientSecret { get; set; }
        /// <summary>
        /// Gets or sets the Redirect URL for Fitbit OAuth authentication.
        /// </summary>
        public required Uri RedirectURL { get; set; }
    }
}