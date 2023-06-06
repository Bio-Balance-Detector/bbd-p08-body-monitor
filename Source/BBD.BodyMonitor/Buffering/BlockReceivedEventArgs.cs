namespace BBD.BodyMonitor.Buffering
{
    public class BlockReceivedEventArgs
    {
        public BlockReceivedEventArgs(ShiftingBuffer buffer, DataBlock dataBlock)
        {
            Buffer = buffer;
            DataBlock = dataBlock;
        }
        public ShiftingBuffer Buffer { get; }
        public DataBlock DataBlock { get; }
    }
}
