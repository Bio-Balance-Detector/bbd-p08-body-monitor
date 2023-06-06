using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Models.ThingSpeak
{
    internal class FeedsResponse
    {
        [JsonPropertyName("channel")]
        public Channel Channel { get; set; }
        [JsonPropertyName("feeds")]
        public Feed[] Feeds { get; set; }
    }
}