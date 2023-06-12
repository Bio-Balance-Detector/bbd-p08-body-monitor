namespace BBD.BodyMonitor.Environment
{
    public class ConnectedDevice
    {
        public required string Brand { get; set; }
        public required string Library { get; set; }
        public int Index { get; set; }
        public int Id { get; set; }
        public int Revision { get; set; }
        public required string Name { get; set; }
        public bool IsOpened { get; set; }
        public required string UserName { get; set; }
        public required string SerialNumber { get; set; }
        public Guid? LastSessionId { get; set; }
    }
}
