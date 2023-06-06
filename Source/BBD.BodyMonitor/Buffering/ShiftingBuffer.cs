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

        private float[] buffer;
        private double samplerate;

        private Dictionary<long, DataBlock> blockRepo = new Dictionary<long, DataBlock>();

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
            this.BlockSize = blockSize;
            this.samplerate = samplerate;
        }

        public void Clear()
        {
            lock (buffer)
            {
                this.blockRepo.Clear();
                this.Position = 0;
                this.TotalWrites = 0;
            }
        }

        public void Error(int bytesAvailable, int bytesLost, int bytesCorrupted, int bytesTotal)
        {
            this.Clear();
            this.BufferError?.Invoke(this, new BufferErrorEventArgs(this, bytesAvailable, bytesLost, bytesCorrupted, bytesTotal));
        }

        public long Write(float[] samplesToWrite)
        {
            lock (buffer)
            {
                long bufferLength = buffer.Length;
                long samplesToWriteLength = samplesToWrite.Length;

                if (this.Position + samplesToWriteLength > bufferLength)
                {
                    // we are going to have an overflow
                    long samplesToWriteFirst = bufferLength - this.Position;
                    Array.Copy(samplesToWrite, 0, buffer, this.Position, samplesToWriteFirst);

                    long samplesToWriteSecond = samplesToWriteLength - samplesToWriteFirst;
                    Array.Copy(samplesToWrite, samplesToWriteFirst, buffer, 0, samplesToWriteSecond);
                    this.Position = samplesToWriteSecond;
                }
                else
                {
                    // we don't have an overflow, so it's fairly straigthforward
                    Array.Copy(samplesToWrite, 0, buffer, this.Position, samplesToWriteLength);
                    this.Position += samplesToWriteLength;
                }

                long previousBlockIndex = (this.TotalWrites / this.BlockSize);
                long currentBlockIndex = ((this.TotalWrites + samplesToWriteLength) / this.BlockSize);

                double blockTime = this.BlockSize * (1000 / this.samplerate);
                DateTimeOffset endTime = DateTimeOffset.Now;
                for (long blockIndex = currentBlockIndex; blockIndex > previousBlockIndex; blockIndex--)
                {
                    // we have at least one new block                    
                    long blockPosition = (blockIndex * this.BlockSize) % buffer.Length;
                    DateTimeOffset startTime = endTime.AddMilliseconds(-blockTime);

                    DataBlock newBlock = new DataBlock(this.BlockSize, blockPosition, blockIndex, blockIndex, this.Get(this.BlockSize, blockPosition), startTime, endTime);
                    blockRepo.Add(blockIndex, newBlock);

                    endTime = startTime;

                    this.BlockReceived?.Invoke(this, new BlockReceivedEventArgs(this, newBlock));
                }

                this.TotalWrites += samplesToWriteLength;
            }

            return this.Position;
        }

        public DataBlock GetBlocks(float duration, long? endBlockIndex = null)
        {
            int blockCount = (int)(duration * this.samplerate / this.BlockSize) + 1;

            return this.GetBlocks(blockCount, endBlockIndex);
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
                List<float> data = new List<float>();

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

                result = new DataBlock(this.BlockSize, -1, startBlockIndex, endBlockIndex.Value, data.ToArray(), startTime, endTime);
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
            bufferPosition ??= this.Position;
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
