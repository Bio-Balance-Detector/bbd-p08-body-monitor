namespace BBD.BodyMonitor.Buffering
{
    public class BufferErrorEventArgs
    {
        public ShiftingBuffer Buffer { get; }
        public int BytesAvailable { get; }
        public int BytesLost { get; }
        public int BytesCorrupted { get; }
        public int BytesTotal { get; }

        public BufferErrorEventArgs(ShiftingBuffer buffer, int bytesAvailable, int bytesLost, int bytesCorrupted, int bytesTotal)
        {
            this.Buffer = buffer;
            this.BytesAvailable = bytesAvailable;
            this.BytesLost = bytesLost;
            this.BytesCorrupted = bytesCorrupted;
            this.BytesTotal = bytesTotal;
        }
    }
}