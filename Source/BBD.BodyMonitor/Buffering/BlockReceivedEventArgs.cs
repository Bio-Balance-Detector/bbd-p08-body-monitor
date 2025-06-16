using BBD.BodyMonitor.Sessions;

namespace BBD.BodyMonitor.Buffering
{
    /// <summary>
    /// Provides data for the <see cref="ShiftingBuffer.BlockReceived"/> event.
    /// </summary>
    public class BlockReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockReceivedEventArgs"/> class.
        /// </summary>
        /// <param name="buffer">The buffer that received the block.</param>
        /// <param name="dataBlock">The data block that was received.</param>
        /// <param name="session">The session associated with the data block.</param>
        /// <param name="error">An optional <see cref="BlockError"/> if an error occurred.</param>
        public BlockReceivedEventArgs(ShiftingBuffer buffer, DataBlock dataBlock, Session session, BlockError? error)
        {
            Buffer = buffer;
            DataBlock = dataBlock;
            Session = session;
            Error = error;
        }
        /// <summary>
        /// Gets the buffer that received the block.
        /// </summary>
        public ShiftingBuffer Buffer { get; }
        /// <summary>
        /// Gets the data block that was received.
        /// </summary>
        public DataBlock DataBlock { get; }
        /// <summary>
        /// Gets the session associated with the data block.
        /// </summary>
        public Session Session { get; }
        /// <summary>
        /// Gets the <see cref="BlockError"/> if an error occurred, otherwise null.
        /// </summary>
        public BlockError? Error { get; }
    }
}
