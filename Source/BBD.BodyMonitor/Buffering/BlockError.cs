namespace BBD.BodyMonitor.Buffering
{
    public class BlockError
    {
        public string Message { get; }
        public int ErrorCount { get; set; }
        public int CorruptedBytes { get; set; }
        public int LostBytes { get; set; }

        public BlockError(string message, int corruptedBytes, int lostBytes)
        {
            Message = message;
            ErrorCount = 1;
            CorruptedBytes = corruptedBytes;
            LostBytes = lostBytes;
        }
    }
}