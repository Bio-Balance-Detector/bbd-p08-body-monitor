using System.Text.Json.Serialization;

namespace BBD.BodyMonitor.Models.ThingSpeak
{
    /// <summary>
    /// Represents the response from a ThingSpeak API request for channel feeds.
    /// It includes information about the channel and an array of feed entries.
    /// This class is intended for internal use within the application.
    /// </summary>
    internal class FeedsResponse
    {
        /// <summary>
        /// Gets or sets the metadata of the ThingSpeak channel to which the feeds belong.
        /// </summary>
        [JsonPropertyName("channel")]
        public Channel Channel { get; set; }

        /// <summary>
        /// Gets or sets an array of feed entries from the ThingSpeak channel.
        /// Each <see cref="Feed"/> object represents a single data point or entry in the channel's history.
        /// </summary>
        [JsonPropertyName("feeds")]
        public Feed[] Feeds { get; set; }
    }
}