using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Models.ThingSpeak
{    
    /// <summary>
    /// Represents a ThingSpeak channel, including its metadata and field descriptions.
    /// </summary>
    public class Channel
    {
        /// <summary>
        /// Gets or sets the unique identifier of the ThingSpeak channel.
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name of the ThingSpeak channel.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the latitude coordinate of the ThingSpeak channel's location.
        /// </summary>
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude coordinate of the ThingSpeak channel's location.
        /// </summary>
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        /// <summary>
        /// Gets or sets the description or name of data field 1 for the channel. Null if not used.
        /// </summary>
        [JsonPropertyName("field1")]
        public string? Field1 { get; set; }

        /// <summary>
        /// Gets or sets the description or name of data field 2 for the channel. Null if not used.
        /// </summary>
        [JsonPropertyName("field2")]        
        public string? Field2 { get; set; }

        /// <summary>
        /// Gets or sets the description or name of data field 3 for the channel. Null if not used.
        /// </summary>
        [JsonPropertyName("field3")]
        public string? Field3 { get; set; }

        /// <summary>
        /// Gets or sets the description or name of data field 4 for the channel. Null if not used.
        /// </summary>
        [JsonPropertyName("field4")]
        public string? Field4 { get; set; }
        // Note: ThingSpeak supports up to 8 fields (field1-field8).
        // This model currently only explicitly defines up to field4.
        // Additional fields would follow the same pattern if needed.

        /// <summary>
        /// Gets or sets the date and time when the channel was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the channel metadata was last updated.
        /// </summary>
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the last entry made to this channel.
        /// </summary>
        [JsonPropertyName("last_entry_id")]
        public int LastEntryId { get; set; }
    }
}