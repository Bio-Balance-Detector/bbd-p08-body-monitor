namespace BBD.BodyMonitor.Buffering
{
    /// <summary>
    /// Represents a block of data received from a device.
    /// </summary>
    public class DataBlock
    {
        /// <summary>
        /// Gets the number of samples in one block.
        /// </summary>
        public long BlockSize { get; }
        /// <summary>
        /// Gets the position of the block in the buffer.
        /// </summary>
        public long BufferPosition { get; }
        /// <summary>
        /// Gets the start index of the data block.
        /// </summary>
        public long StartIndex { get; }
        /// <summary>
        /// Gets the end index of the data block.
        /// </summary>
        public long EndIndex { get; }
        /// <summary>
        /// Gets the merged data array from <see cref="StartIndex"/> to <see cref="EndIndex"/>.
        /// </summary>
        public float[] Data { get; }
        /// <summary>
        /// Gets the timestamp of when the first block started.
        /// </summary>
        public DateTimeOffset StartTime { get; }
        /// <summary>
        /// Gets the timestamp of when the last block ended.
        /// </summary>
        public DateTimeOffset EndTime { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataBlock"/> class.
        /// </summary>
        /// <param name="blockSize">The number of samples in one block.</param>
        /// <param name="bufferPosition">The position of the block in the buffer.</param>
        /// <param name="startIndex">The start index of the data block.</param>
        /// <param name="endIndex">The end index of the data block.</param>
        /// <param name="data">The merged data array.</param>
        /// <param name="startTime">The timestamp of when the first block started.</param>
        /// <param name="endTime">The timestamp of when the last block ended.</param>
        public DataBlock(long blockSize, long bufferPosition, long startIndex, long endIndex, float[] data, DateTimeOffset startTime, DateTimeOffset endTime)
        {
            BlockSize = blockSize;
            BufferPosition = bufferPosition;
            StartIndex = startIndex;
            EndIndex = endIndex;
            Data = data;
            StartTime = startTime;
            EndTime = endTime;
        }

        /// <summary>
        /// Calculates a hash code for the current <see cref="DataBlock"/> instance.
        /// </summary>
        /// <returns>A hash code for the current <see cref="DataBlock"/> instance.</returns>
        public long GetHash()
        {
            return BlockSize ^ BufferPosition ^ StartIndex ^ EndIndex ^ StartTime.ToUnixTimeMilliseconds() ^ EndTime.ToUnixTimeMilliseconds();
        }
    }
}
