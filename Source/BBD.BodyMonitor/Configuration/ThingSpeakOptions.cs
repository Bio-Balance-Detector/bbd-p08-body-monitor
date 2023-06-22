namespace BBD.BodyMonitor.Configuration
{
    public class ThingSpeakOptions
    {
        public Uri APIEndpoint { get; set; } = new Uri("https://api.thingspeak.com");
        public string? APIKey { get; set; }
    }
}