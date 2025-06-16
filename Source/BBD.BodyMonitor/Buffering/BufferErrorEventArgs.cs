namespace BBD.BodyMonitor.Buffering
{
    /// <summary>
    /// Provides data for buffer error events.
    /// </summary>
    public class BufferErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the buffer that experienced the error.
        /// </summary>
        public ShiftingBuffer Buffer { get; }
        /// <summary>
        /// Gets the number of bytes available in the buffer.
        /// </summary>
        public int BytesAvailable { get; }
        /// <summary>
        /// Gets the number of bytes lost.
        /// </summary>
        public int BytesLost { get; }
        /// <summary>
        /// Gets the number of bytes corrupted.
        /// </summary>
        public int BytesCorrupted { get; }
        /// <summary>
        /// Gets the total number of bytes processed or expected.
        /// </summary>
        public int BytesTotal { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferErrorEventArgs"/> class.
        /// </summary>
        /// <param name="buffer">The buffer that experienced the error.</param>
        /// <param name="bytesAvailable">The number of bytes available in the buffer.</param>
        /// <param name="bytesLost">The number of bytes lost.</param>
        /// <param name="bytesCorrupted">The number of bytes corrupted.</param>
        /// <param name="bytesTotal">The total number of bytes processed or expected.</param>
        public BufferErrorEventArgs(ShiftingBuffer buffer, int bytesAvailable, int bytesLost, int bytesCorrupted, int bytesTotal)
        {
            Buffer = buffer;
            BytesAvailable = bytesAvailable;
            BytesLost = bytesLost;
            BytesCorrupted = bytesCorrupted;
            BytesTotal = bytesTotal;
        }
    }
}