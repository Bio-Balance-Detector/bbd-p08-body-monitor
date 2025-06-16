namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Configuration options for a server connection.
    /// </summary>
    public class ServerOptions
    {
        /// <summary>
        /// Gets or sets the name of the server.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the network address of the server (e.g., IP address or hostname).
        /// </summary>
        public string Address { get; set; } = string.Empty;
    }
}