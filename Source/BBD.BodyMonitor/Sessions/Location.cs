namespace BBD.BodyMonitor.Sessions
{
    public class Location : DeidentifiedData
    {
        public string PlusCode { get; set; }
        public string TimeZone { get; set; }
        public override string ToString()
        {
            return $"Location {this.Alias}: '{this.Name}'";
        }
    }
}
