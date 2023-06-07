namespace BBD.BodyMonitor.Configuration
{
    public class FitbitOptions
    {
        public string? ClientID { get; set; }
        public string? ClientSecret { get; set; }
        public required Uri RedirectURL { get; set; }
    }
}