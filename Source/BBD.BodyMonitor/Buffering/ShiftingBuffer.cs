namespace BBD.BodyMonitor.Buffering
{
﻿namespace BBD.BodyMonitor.Buffering
{
    /// <summary>
    /// Defines how the buffer handles errors.
    /// </summary>
    public enum BufferErrorHandlingMode
    {
        /// <summary>
        /// Clears the entire buffer when an error occurs.
        /// </summary>
        ClearBuffer,
        /// <summary>
        /// Discards only the corrupted samples.
        /// </summary>
        DiscardSamples,
        /// <summary>
        /// Replaces corrupted samples with zero values.
        /// </summary>
        ZeroSamples
    }

    /// <summary>
    /// Represents a shifting circular buffer for storing and processing data blocks.
    /// </summary>
    public class ShiftingBuffer
    {
        /// <summary>
        /// Gets the total number of float samples written to the buffer.
        /// </summary>
        public long TotalWrites { get; private set; }
        /// <summary>
        /// Gets the current write position in the buffer.
        /// </summary>
        public long Position { get; private set; }
        /// <summary>
        /// Gets the number of float samples in one data block.
        /// </summary>
        public long BlockSize { get; private set; }
        /// <summary>
        /// Gets or sets the error handling mode for the buffer.
        /// </summary>
        public BufferErrorHandlingMode ErrorHandlingMode { get; set; } = BufferErrorHandlingMode.ClearBuffer;

        /// <summary>
        /// Represents the method that will handle the <see cref="BlockReceived"/> event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BlockReceivedEventArgs"/> that contains the event data.</param>
        public delegate void BlockReceivedEventHandler(object sender, BlockReceivedEventArgs e);

        /// <summary>
        /// Represents the method that will handle a buffer error event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">A <see cref="BufferErrorEventArgs"/> that contains the event data.</param>
        public delegate void BufferErrorEventHandler(object sender, BufferErrorEventArgs e);

        /// <summary>
        /// Occurs when a new data block is received and processed.
        /// </summary>
        public event BlockReceivedEventHandler? BlockReceived;
        /// <summary>
        /// Occurs when an error is encountered in the buffer.
        /// </summary>
        public event BufferErrorEventHandler? BufferError;

        /// <summary>
        /// The internal buffer that stores the data.
        /// </summary>
        private readonly float[] buffer;
        /// <summary>
        /// The sample rate of the data in Hz.
        /// </summary>
        private readonly double samplerate;
        /// <summary>
        /// A repository of the data blocks, indexed by their block index.
        /// </summary>
        private readonly Dictionary<long, DataBlock> blockRepo = new();
        private BlockError? _lastError;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftingBuffer"/> class.
        /// </summary>
        /// <param name="bufferSize">The total number of float samples the buffer can hold. Must be a multiple of <paramref name="blockSize"/>.</param>
        /// <param name="blockSize">The number of float samples in each data block. Must be greater than zero.</param>
        /// <param name="samplerate">The sampling rate of the data in Hz. Must be greater than zero.</param>
        /// <exception cref="System.ArgumentException">
        /// Thrown if <paramref name="bufferSize"/> is less than <paramref name="blockSize"/>,
        /// or if <paramref name="blockSize"/> is not greater than zero,
        /// or if <paramref name="bufferSize"/> is not a multiple of <paramref name="blockSize"/>,
        /// or if <paramref name="samplerate"/> is not greater than zero.
        /// </exception>
        public ShiftingBuffer(long bufferSize, long blockSize, double samplerate)
        {
            if (bufferSize < blockSize)
            {
                throw new System.ArgumentException("Buffer size must be greater than block size.", nameof(bufferSize));
            }

            if (blockSize <= 0)
            {
                throw new System.ArgumentException("Blocksize must be bigger than zero.", nameof(blockSize));
            }

            if (bufferSize % blockSize != 0)
            {
                throw new System.ArgumentException("Block size must be a multiple of buffer size.", nameof(blockSize));
            }

            if (samplerate <= 0)
            {
                throw new System.ArgumentException("Samplerate must be bigger than zero.", nameof(samplerate));
            }

            buffer = new float[bufferSize];
            BlockSize = blockSize;
            this.samplerate = samplerate;
        }

        /// <summary>
        /// Clears the buffer, resetting its position and total writes.
        /// </summary>
        public void Clear()
        {
            lock (buffer)
            {
                blockRepo.Clear();
                Position = 0;
                TotalWrites = 0;
                _lastError = null;
            }
        }

        /// <summary>
        /// Handles errors encountered in the buffer based on the <see cref="ErrorHandlingMode"/>.
        /// </summary>
        /// <param name="bytesAvailable">The number of bytes available in the previous data pull.</param>
        /// <param name="bytesLost">The number of bytes lost in the previous data pull.</param>
        /// <param name="bytesCorrupted">The number of bytes corrupted in the previous data pull.</param>
        /// <param name="bytesTotal">The total number of bytes in the previous data pull.</param>
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
                // Currently, no specific action is taken here as samples are processed upon writing.
                // This mode implies that errors are noted, and subsequent writes might fill in gaps or corrupted data is ignored.
            }

            // We replace the samples with 0
            if (ErrorHandlingMode == BufferErrorHandlingMode.ZeroSamples)
            {
                // Writes an array of zeros corresponding to the total bytes of the problematic data segment.
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
        /// Writes the given samples to the buffer.
        /// </summary>
        /// <param name="samplesToWrite">The array of float samples to write to the buffer.</param>
        /// <returns>The current position in the buffer after the write operation.</returns>
        public long Write(float[] samplesToWrite)
        {
            lock (buffer)
            {
                long bufferLength = buffer.Length;
                long samplesToWriteLength = samplesToWrite.Length;

                if (Position + samplesToWriteLength > bufferLength)
                {
                    // Handle buffer overflow by wrapping around.
                    long samplesToWriteFirst = bufferLength - Position;
                    Array.Copy(samplesToWrite, 0, buffer, Position, samplesToWriteFirst);

                    long samplesToWriteSecond = samplesToWriteLength - samplesToWriteFirst;
                    Array.Copy(samplesToWrite, samplesToWriteFirst, buffer, 0, samplesToWriteSecond);
                    Position = samplesToWriteSecond;
                }
                else
                {
                    // No overflow, copy directly.
                    Array.Copy(samplesToWrite, 0, buffer, Position, samplesToWriteLength);
                    Position += samplesToWriteLength;
                }

                long previousBlockIndex = TotalWrites / BlockSize;
                long currentBlockIndex = (TotalWrites + samplesToWriteLength) / BlockSize;

                double blockTime = BlockSize * (1000 / samplerate); // Duration of a block in milliseconds.
                DateTimeOffset endTime = DateTimeOffset.Now;
                for (long blockIndex = currentBlockIndex; blockIndex > previousBlockIndex; blockIndex--)
                {
                    // A new block has been completely written.
                    long blockBufferPosition = blockIndex * BlockSize % buffer.Length; // Position of the start of this block in the circular buffer.
                    DateTimeOffset startTime = endTime.AddMilliseconds(-blockTime);

                    // Extract the data for the new block.
                    float[] blockData = Get(BlockSize, (blockBufferPosition + BlockSize) % bufferLength); // Get data ending at the current block's end position
                    DataBlock newBlock = new(BlockSize, blockBufferPosition, blockIndex, blockIndex, blockData, startTime, endTime);
                    blockRepo[blockIndex] = newBlock; // Add or update the block in the repository.

                    endTime = startTime; // The end time for the next older block is the start time of this one.

                    BlockReceived?.Invoke(this, new BlockReceivedEventArgs(this, newBlock, null, _lastError)); // TODO: Session is null
                    _lastError = null; // Reset error after a successful block processing.
                }

                TotalWrites += samplesToWriteLength;
            }

            return Position;
        }

        /// <summary>
        /// Retrieves a <see cref="DataBlock"/> containing data for a specified duration, ending at a specific block index.
        /// </summary>
        /// <param name="duration">The duration in seconds of data to retrieve.</param>
        /// <param name="endBlockIndex">Optional. The index of the last block to include. If null, the latest available block is used.</param>
        /// <returns>A <see cref="DataBlock"/> containing the requested data.</returns>
        public DataBlock GetBlocks(float duration, long? endBlockIndex = null)
        {
            // Calculate the number of blocks based on duration and sample rate.
            int blockCount = (int)(duration * samplerate / BlockSize) + 1;

            return GetBlocks(blockCount, endBlockIndex);
        }

        /// <summary>
        /// Retrieves a <see cref="DataBlock"/> containing a specified number of blocks, ending at a specific block index.
        /// </summary>
        /// <param name="blockCount">The number of blocks to retrieve.</param>
        /// <param name="endBlockIndex">Optional. The index of the last block to include. If null, the latest available block is used.</param>
        /// <returns>A <see cref="DataBlock"/> containing the requested data, or an empty DataBlock if data is not available.</returns>
        public DataBlock GetBlocks(int blockCount, long? endBlockIndex = null)
        {
            DataBlock result;

            lock (buffer)
            {
                if (blockRepo.Count == 0)
                {
                    return new DataBlock(BlockSize, -1, 0, 0, Array.Empty<float>(), DateTimeOffset.MinValue, DateTimeOffset.MinValue);
                }

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
                // If no data was found (e.g. requested blocks are not in repo), return an empty DataBlock with sensible defaults.
                if (data.Count == 0)
                {
                    return new DataBlock(BlockSize, -1, startBlockIndex, endBlockIndex.Value, Array.Empty<float>(), DateTimeOffset.MinValue, DateTimeOffset.MinValue);
                }

                result = new DataBlock(BlockSize, -1, startBlockIndex, endBlockIndex.Value, data.ToArray(), startTime, endTime);
            }

            return result;
        }

        /// <summary>
        /// Gets the specified number of float samples from the buffer.
        /// </summary>
        /// <param name="samplesToRead">Number of float samples to read. Must not exceed buffer length.</param>
        /// <param name="bufferPosition">The ending position in the buffer from which to read backwards. If null, the current <see cref="Position"/> is used.</param>
        /// <returns>An array of float samples that were requested.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="samplesToRead"/> is greater than the buffer length.</exception>
        private float[] Get(long samplesToRead, long? bufferPosition = null)
        {
            if (samplesToRead > buffer.Length)
            {
                throw new ArgumentException($"The maximum number of samples available to read is {buffer.Length}, but you are trying to read {samplesToRead}. Lower the requested number of samples, or increase the buffer size.", nameof(samplesToRead));
            }

            int bufferLength = buffer.Length;
            float[] result = new float[samplesToRead];
            // If bufferPosition is null, use current Position. This means we read the last 'samplesToRead' samples written.
            long endReadPosition = bufferPosition ?? Position;
            // Calculate the effective start position in the circular buffer.
            long readStartPosition = (endReadPosition - samplesToRead + bufferLength) % bufferLength;

            lock (buffer)
            {
                if (readStartPosition + samplesToRead > bufferLength)
                {
                    // Read wraps around the end of the buffer.
                    long samplesToReadFirst = bufferLength - readStartPosition;
                    Array.Copy(buffer, readStartPosition, result, 0, samplesToReadFirst);

                    long samplesToReadSecond = samplesToRead - samplesToReadFirst;
                    Array.Copy(buffer, 0, result, samplesToReadFirst, samplesToReadSecond);
                }
                else
                {
                    // Read is contiguous within the buffer.
                    Array.Copy(buffer, readStartPosition, result, 0, samplesToRead);
                }
            }

            return result;
        }
    }
}
