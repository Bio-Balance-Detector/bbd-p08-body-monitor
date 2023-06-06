namespace BBD.BodyMonitor.Sessions
{
    public class Subject : DeidentifiedData
    {
        public string? Gender { get; set; }
        public DateTime? Birthdate { get; set; }
        public float? Weight { get; set; }
        public float? Height { get; set; }
        public string? FitbitEncodedID { get; set; }
        public string[]? Conditions { get; set; }
        public Identity? Identity { get; set; }

        public override string ToString()
        {
            return $"Subject {this.Alias}: '{this.Name}'";
        }
    }
}