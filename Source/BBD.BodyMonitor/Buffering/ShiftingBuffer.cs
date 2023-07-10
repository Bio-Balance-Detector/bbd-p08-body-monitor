namespace BBD.BodyMonitor.Buffering
{
    /// <summary>
    /// Enum that defines how the buffer handles errors
    /// </summary>
    public enum BufferErrorHandlingMode
    {
        /// <summary>
        /// Clears the buffer
        /// </summary>
        ClearBuffer,
        /// <summary>
        /// Discards the samples that are corrupted
        /// </summary>
        DiscardSamples,
        /// <summary>
        /// Replaces the samples that are corrupted with 0
        /// </summary>
        ZeroSamples
    }

    /// <summary>
    /// Shifting buffer that can be used to store data in a circular buffer.
    /// </summary>
    public class ShiftingBuffer
    {
        /// <summary>
        /// Total number of (float) samples writen to the buffer
        /// </summary>
        public long TotalWrites { get; private set; }
        /// <summary>
        /// Current position in the buffer
        /// </summary>
        public long Position { get; private set; }
        /// <summary>
        /// Number of (float) samples in one block
        /// </summary>
        public long BlockSize { get; private set; }
        public BufferErrorHandlingMode ErrorHandlingMode { get; set; } = BufferErrorHandlingMode.ClearBuffer;

        /// <summary>
        /// Delegate for the event that is fired when a block is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void BlockReceivedEventHandler(object sender, BlockReceivedEventArgs e);

        /// <summary>
        /// Delegate for the event that is fired when a buffer error occurs
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public delegate void BufferErrorEventHandler(object sender, BufferErrorEventArgs e);

        /// <summary>
        /// Event that is fired when a block is received
        /// </summary>
        public event BlockReceivedEventHandler? BlockReceived;
        /// <summary>
        /// Event that is fired when a buffer error occurs
        /// </summary>
        public event BufferErrorEventHandler? BufferError;

        /// <summary>
        /// Buffer that stores the data
        /// </summary>
        private readonly float[] buffer;
        /// <summary>
        /// Samplerate of the data
        /// </summary>
        private readonly double samplerate;
        /// <summary>
        /// Repository of the data blocks
        /// </summary>
        private readonly Dictionary<long, DataBlock> blockRepo = new();
        private BlockError? _lastError;

        /// <summary>
        /// Creates a new shifting buffer
        /// </summary>
        /// <param name="bufferSize">Number of (float) samples in the buffer</param>
        /// <param name="blockSize">Number of (float) samples in a block</param>
        /// <param name="samplerate">Sampling rate</param>
        /// <exception cref="System.ArgumentException"></exception>
        public ShiftingBuffer(long bufferSize, long blockSize, double samplerate)
        {
            if (bufferSize < blockSize)
            {
                throw new System.ArgumentException("Buffer size must be greater than block size.", "bufferSize");
            }

            if (blockSize <= 0)
            {
                throw new System.ArgumentException("Blocksize must be bigger than zero.", "blockSize");
            }

            if (bufferSize % blockSize != 0)
            {
                throw new System.ArgumentException("Block size must be a multiple of buffer size.", "blockSize");
            }

            if (samplerate <= 0)
            {
                throw new System.ArgumentException("Samplerate must be bigger than zero.", "samplerate");
            }

            buffer = new float[bufferSize];
            BlockSize = blockSize;
            this.samplerate = samplerate;
        }

        /// <summary>
        /// Creates a new shifting buffer
        /// </summary>
        public void Clear()
        {
            lock (buffer)
            {
                blockRepo.Clear();
                Position = 0;
                TotalWrites = 0;
            }
        }

        /// <summary>
        /// Error handling for the buffer
        /// </summary>
        /// <param name="bytesAvailable">Available bytes in the previous data pull</param>
        /// <param name="bytesLost">Lost bytes in the previous data pull</param>
        /// <param name="bytesCorrupted">Corrupted bytes in the previous data pull</param>
        /// <param name="bytesTotal">Total bytes in the previous data pull</param>
        public void Error(int bytesAvailable, int bytesLost, int bytesCorrupted, int bytesTotal)
        {
            if (_lastError != null)
            {
                _lastError.ErrorCount++;
                _lastError.CorruptedBytes += bytesCorrupted;
                _lastError.LostBytes += bytesLost;
            }
            else
            {
                _lastError = new BlockError("Error in buffer", bytesCorrupted, bytesLost);
            }

            // We discard the samples
            if (ErrorHandlingMode == BufferErrorHandlingMode.DiscardSamples)
            {

            }

            // We replace the samples with 0
            if (ErrorHandlingMode == BufferErrorHandlingMode.ZeroSamples)
            {
                _ = Write(new float[bytesTotal]);
            }

            BufferError?.Invoke(this, new BufferErrorEventArgs(this, bytesAvailable, bytesLost, bytesCorrupted, bytesTotal));

            // We just clear the buffer
            if (ErrorHandlingMode == BufferErrorHandlingMode.ClearBuffer)
            {
                Clear();
            }
        }

        /// <summary>
        /// Writes the given samples to the buffer
        /// </summary>
        /// <param name="samplesToWrite">Samples to write to the buffer</param>
        /// <returns>Current position in the buffer</returns>
        public long Write(float[] samplesToWrite)
        {
            lock (buffer)
            {
                long bufferLength = buffer.Length;
                long samplesToWriteLength = samplesToWrite.Length;

                if (Position + samplesToWriteLength > bufferLength)
                {
                    // we are going to have an overflow
                    long samplesToWriteFirst = bufferLength - Position;
                    Array.Copy(samplesToWrite, 0, buffer, Position, samplesToWriteFirst);

                    long samplesToWriteSecond = samplesToWriteLength - samplesToWriteFirst;
                    Array.Copy(samplesToWrite, samplesToWriteFirst, buffer, 0, samplesToWriteSecond);
                    Position = samplesToWriteSecond;
                }
                else
                {
                    // we don't have an overflow, so it's fairly straigthforward
                    Array.Copy(samplesToWrite, 0, buffer, Position, samplesToWriteLength);
                    Position += samplesToWriteLength;
                }

                long previousBlockIndex = TotalWrites / BlockSize;
                long currentBlockIndex = (TotalWrites + samplesToWriteLength) / BlockSize;

                double blockTime = BlockSize * (1000 / samplerate);
                DateTimeOffset endTime = DateTimeOffset.Now;
                for (long blockIndex = currentBlockIndex; blockIndex > previousBlockIndex; blockIndex--)
                {
                    // we have at least one new block                    
                    long blockPosition = blockIndex * BlockSize % buffer.Length;
                    DateTimeOffset startTime = endTime.AddMilliseconds(-blockTime);

                    DataBlock newBlock = new(BlockSize, blockPosition, blockIndex, blockIndex, Get(BlockSize, blockPosition), startTime, endTime);
                    blockRepo.Add(blockIndex, newBlock);

                    endTime = startTime;

                    BlockReceived?.Invoke(this, new BlockReceivedEventArgs(this, newBlock, null, _lastError));
                    _lastError = null;
                }

                TotalWrites += samplesToWriteLength;
            }

            return Position;
        }

        /// <summary>
        /// Gets the given duration of blocks from the buffer
        /// </summary>
        /// <param name="duration">Duration in seconds to get</param>
        /// <param name="endBlockIndex">The last block we want to get</param>
        /// <returns></returns>
        public DataBlock GetBlocks(float duration, long? endBlockIndex = null)
        {
            int blockCount = (int)(duration * samplerate / BlockSize) + 1;

            return GetBlocks(blockCount, endBlockIndex);
        }

        /// <summary>
        /// Gets the given number of blocks from the buffer
        /// </summary>
        /// <param name="blockCount">Number of blocks to get</param>
        /// <param name="endBlockIndex">The last block we want to get</param>
        /// <returns></returns>
        public DataBlock GetBlocks(int blockCount, long? endBlockIndex = null)
        {
            DataBlock result;

            lock (buffer)
            {
                endBlockIndex ??= blockRepo.Keys.Last();
                long startBlockIndex = endBlockIndex.Value - blockCount + 1;

                DateTimeOffset startTime = DateTimeOffset.MaxValue;
                DateTimeOffset endTime = DateTimeOffset.MinValue;
                List<float> data = new();

                for (long i = startBlockIndex; i <= endBlockIndex.Value; i++)
                {
                    if (blockRepo.TryGetValue(i, out DataBlock db))
                    {
                        if (db.StartTime < startTime)
                        {
                            startTime = db.StartTime;
                        }

                        if (db.EndTime > endTime)
                        {
                            endTime = db.EndTime;
                        }

                        data.AddRange(db.Data);
                    }
                }

                result = new DataBlock(BlockSize, -1, startBlockIndex, endBlockIndex.Value, data.ToArray(), startTime, endTime);
            }

            return result;
        }

        /// <summary>
        /// Gets the given number of (float) samples from the buffer
        /// </summary>
        /// <param name="samplesToRead">Number of (float) samples to read</param>
        /// <param name="bufferPosition">The starting position of the read. If null, the current position is used.</param>
        /// <returns>Array of (float) samples that were requested</returns>
        /// <exception cref="ArgumentException"></exception>
        private float[] Get(long samplesToRead, long? bufferPosition = null)
        {
            if (samplesToRead > buffer.Length)
            {
                throw new ArgumentException($"The maximum number of samples available to read is {buffer.Length}, but you are trying to read {samplesToRead}. Lower the requested number of samples, or increase the buffer size.", "samplesToRead");
            }

            int bufferLength = buffer.Length;
            float[] result = new float[samplesToRead];
            bufferPosition ??= Position;
            long readStartPosition = (bufferPosition.Value - samplesToRead + bufferLength) % bufferLength;

            lock (buffer)
            {
                if (readStartPosition + samplesToRead > bufferLength)
                {
                    // we are going to have an overflow
                    long samplesToReadFirst = bufferLength - readStartPosition;
                    Array.Copy(buffer, readStartPosition, result, 0, samplesToReadFirst);

                    long samplesToReadSecond = samplesToRead - samplesToReadFirst;
                    Array.Copy(buffer, 0, result, samplesToReadFirst, samplesToReadSecond);
                }
                else
                {
                    // we don't have an overflow, so it's fairly straigthforward
                    Array.Copy(buffer, readStartPosition, result, 0, samplesToRead);
                }
            }

            return result;
        }
    }
}
