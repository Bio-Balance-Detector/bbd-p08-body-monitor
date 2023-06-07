namespace BBD.BodyMonitor.Buffering
{
    public class DataBlock
    {
        /// <summary>
        /// Number of samples in one block
        /// </summary>
        public long BlockSize { get; }
        /// <summary>
        /// The position of the block in the buffer
        /// </summary>
        public long BufferPosition { get; }
        /// <summary>
        /// The first block that this data block includes
        /// </summary>
        public long StartIndex { get; }
        /// <summary>
        /// The last block that this data block includes
        /// </summary>
        public long EndIndex { get; }
        /// <summary>
        /// Merged data array of the blocks from StartIndex to EndIndex
        /// </summary>
        public float[] Data { get; }
        /// <summary>
        /// The time when the first block started
        /// </summary>
        public DateTimeOffset StartTime { get; }
        /// <summary>
        /// The time when the last block ended
        /// </summary>
        public DateTimeOffset EndTime { get; }

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

        public long GetHash()
        {
            return BlockSize ^ BufferPosition ^ StartIndex ^ EndIndex ^ StartTime.ToUnixTimeMilliseconds() ^ EndTime.ToUnixTimeMilliseconds();
        }
    }
}
