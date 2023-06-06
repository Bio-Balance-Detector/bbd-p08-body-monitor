using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class FitbitOptions
    {
        public string? ClientID { get; set; }
        public string? ClientSecret { get; set; }
        public Uri RedirectURL { get; set; }
    }
}