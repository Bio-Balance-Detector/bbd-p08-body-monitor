using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Models.ThingSpeak
{
    public class Feed
    {
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("entry_id")]
        public int EntryId { get; set; }
        [JsonPropertyName("field1")]
        public float? Field1 { get; set; }    
        [JsonPropertyName("field2")]
        public float? Field2 { get; set; }
        [JsonPropertyName("field3")]
        public float? Field3 { get; set; }
        [JsonPropertyName("field4")]
        public float? Field4 { get; set; }
    }
}