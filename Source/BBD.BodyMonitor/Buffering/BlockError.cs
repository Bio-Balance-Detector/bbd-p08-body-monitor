namespace BBD.BodyMonitor.Buffering
{
    /// <summary>
    /// Represents an error that occurred during data block processing.
    /// </summary>
    public class BlockError
    {
        /// <summary>
        /// Gets the error message.
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// Gets or sets the number of times this error has occurred.
        /// </summary>
        public int ErrorCount { get; set; }
        /// <summary>
        /// Gets or sets the number of corrupted bytes.
        /// </summary>
        public int CorruptedBytes { get; set; }
        /// <summary>
        /// Gets or sets the number of lost bytes.
        /// </summary>
        public int LostBytes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockError"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="corruptedBytes">The number of corrupted bytes.</param>
        /// <param name="lostBytes">The number of lost bytes.</param>
        public BlockError(string message, int corruptedBytes, int lostBytes)
        {
            Message = message;
            ErrorCount = 1;
            CorruptedBytes = corruptedBytes;
            LostBytes = lostBytes;
        }
    }
}