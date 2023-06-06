using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Models.ThingSpeak
{    
    public class Channel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
        [JsonPropertyName("field1")]
        public string? Field1 { get; set; }
        [JsonPropertyName("field2")]        
        public string? Field2 { get; set; }
        [JsonPropertyName("field3")]
        public string? Field3 { get; set; }
        [JsonPropertyName("field4")]
        public string? Field4 { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]
        public DateTime UpdatedAt { get; set; }
        [JsonPropertyName("last_entry_id")]
        public int LastEntryId { get; set; }
    }
}