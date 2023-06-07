namespace BBD.BodyMonitor.Sessions
{
    public class Location : DeidentifiedData
    {
        public required string PlusCode { get; set; }
        public required string TimeZone { get; set; }
        public override string ToString()
        {
            return $"Location {Alias}: '{Name}'";
        }
    }
}
