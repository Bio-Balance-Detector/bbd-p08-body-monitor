using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Models.ThingSpeak
{
    /// <summary>
    /// Represents a single data entry (feed) from a ThingSpeak channel.
    /// Each feed contains an entry ID, creation timestamp, and values for the channel's data fields.
    /// </summary>
    public class Feed
    {
        /// <summary>
        /// Gets or sets the date and time when this feed entry was created.
        /// </summary>
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for this feed entry.
        /// </summary>
        [JsonPropertyName("entry_id")]
        public int EntryId { get; set; }

        /// <summary>
        /// Gets or sets the value of data field 1 for this entry. Null if the field is not populated for this entry.
        /// </summary>
        [JsonPropertyName("field1")]
        public float? Field1 { get; set; }    

        /// <summary>
        /// Gets or sets the value of data field 2 for this entry. Null if the field is not populated for this entry.
        /// </summary>
        [JsonPropertyName("field2")]
        public float? Field2 { get; set; }

        /// <summary>
        /// Gets or sets the value of data field 3 for this entry. Null if the field is not populated for this entry.
        /// </summary>
        [JsonPropertyName("field3")]
        public float? Field3 { get; set; }

        /// <summary>
        /// Gets or sets the value of data field 4 for this entry. Null if the field is not populated for this entry.
        /// </summary>
        [JsonPropertyName("field4")]
        public float? Field4 { get; set; }
        // Note: ThingSpeak supports up to 8 fields (field1-field8).
        // This model currently only explicitly defines up to field4.
        // Additional fields would follow the same pattern (e.g., public float? Field5 { get; set; }) if needed.
    }
}