using BBD.BodyMonitor.Sessions;

namespace BBD.BodyMonitor.Buffering
{
    public class BlockReceivedEventArgs
    {
        public BlockReceivedEventArgs(ShiftingBuffer buffer, DataBlock dataBlock, Session session, BlockError? error)
        {
            Buffer = buffer;
            DataBlock = dataBlock;
            Session = session;
            Error = error;
        }
        public ShiftingBuffer Buffer { get; }
        public DataBlock DataBlock { get; }
        public Session Session { get; }
        public BlockError? Error { get; }
    }
}
