namespace BBD.BodyMonitor.Buffering
{
    public class ShiftingBuffer
    {
        public long TotalWrites { get; private set; }
        public long Position { get; private set; }
        public long BlockSize { get; private set; }

        public delegate void BlockReceivedEventHandler(object sender, BlockReceivedEventArgs e);
        public delegate void BufferErrorEventHandler(object sender, BufferErrorEventArgs e);

        public event BlockReceivedEventHandler BlockReceived;
        public event BufferErrorEventHandler BufferError;

        private readonly float[] buffer;
        private readonly double samplerate;

        private readonly Dictionary<long, DataBlock> blockRepo = new();

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

        public void Clear()
        {
            lock (buffer)
            {
                blockRepo.Clear();
                Position = 0;
                TotalWrites = 0;
            }
        }

        public void Error(int bytesAvailable, int bytesLost, int bytesCorrupted, int bytesTotal)
        {
            Clear();
            BufferError?.Invoke(this, new BufferErrorEventArgs(this, bytesAvailable, bytesLost, bytesCorrupted, bytesTotal));
        }

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

                    BlockReceived?.Invoke(this, new BlockReceivedEventArgs(this, newBlock));
                }

                TotalWrites += samplesToWriteLength;
            }

            return Position;
        }

        public DataBlock GetBlocks(float duration, long? endBlockIndex = null)
        {
            int blockCount = (int)(duration * samplerate / BlockSize) + 1;

            return GetBlocks(blockCount, endBlockIndex);
        }

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
