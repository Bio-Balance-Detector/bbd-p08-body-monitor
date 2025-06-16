namespace BBD.BodyMonitor.Sessions
{
    /// <summary>
    /// Represents a geographical location with de-identification, plus code, and time zone information. Inherits from DeidentifiedData.
    /// </summary>
    public class Location : DeidentifiedData
    {
        /// <summary>
        /// Gets or sets the Open Location Code (Plus Code) for the location.
        /// </summary>
        public required string PlusCode { get; set; }
        /// <summary>
        /// Gets or sets the IANA time zone name for the location (e.g., 'Europe/Budapest').
        /// </summary>
        public required string TimeZone { get; set; }
        /// <summary>
        /// Returns a string representation of the location.
        /// </summary>
        /// <returns>A string in the format 'Location Alias: 'Name''.</returns>
        public override string ToString()
        {
            return $"Location {Alias}: '{Name}'";
        }
    }
}
