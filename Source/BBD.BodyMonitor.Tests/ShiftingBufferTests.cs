using Xunit;
using BBD.BodyMonitor.Buffering;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace BBD.BodyMonitor.Tests
{
    public class ShiftingBufferTests
    {
        private const int DefaultBlockSize = 10;
        private const int DefaultBufferSize = DefaultBlockSize * 3;
        private const double DefaultSamplerate = 100.0;

        private float[] GenerateSampleData(int count, int startValue = 0)
        {
            return Enumerable.Range(startValue, count).Select(i => (float)i).ToArray();
        }

        [Theory]
        [InlineData(DefaultBlockSize - 1, DefaultBlockSize, DefaultSamplerate)]
        [InlineData(DefaultBufferSize, 0, DefaultSamplerate)]
        [InlineData(DefaultBufferSize, -1, DefaultSamplerate)]
        [InlineData(DefaultBufferSize, DefaultBlockSize + 1, DefaultSamplerate)] // bufferSize % blockSize != 0
        [InlineData(DefaultBufferSize, DefaultBlockSize, 0)]
        [InlineData(DefaultBufferSize, DefaultBlockSize, -1.0)]
        public void Constructor_InvalidArguments_ThrowsArgumentException(long bufferSize, long blockSize, double samplerate)
        {
            Assert.Throws<ArgumentException>(() => new ShiftingBuffer(bufferSize, blockSize, samplerate));
        }

        [Fact]
        public void Constructor_ValidArguments_InitializesCorrectly()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            Assert.Equal(0, buffer.Position);
            Assert.Equal(0, buffer.TotalWrites);
            Assert.Equal(DefaultBlockSize, buffer.BlockSize);
            Assert.Equal(BufferErrorHandlingMode.ClearBuffer, buffer.ErrorHandlingMode); // Default
            // To check blockRepo initialization, call GetBlocks for an empty state
            var emptyBlock = buffer.GetBlocks(1, 0); // Request 1 block ending at index 0
            Assert.Empty(emptyBlock.Data);
            Assert.Equal(-1, emptyBlock.BufferPosition); // As per ShiftingBuffer.GetBlocks empty case
        }

        [Fact]
        public void Write_SingleBlock_RaisesBlockReceivedWithCorrectData()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            var samples = GenerateSampleData(DefaultBlockSize);
            BlockReceivedEventArgs? receivedEventArgs = null;
            int eventCount = 0;

            buffer.BlockReceived += (sender, args) => {
                receivedEventArgs = args;
                eventCount++;
            };

            buffer.Write(samples);

            Assert.Equal(1, eventCount);
            Assert.NotNull(receivedEventArgs);
            Assert.NotNull(receivedEventArgs.DataBlock);
            var dataBlock = receivedEventArgs.DataBlock;

            Assert.Equal(DefaultBlockSize, dataBlock.BlockSize);
            Assert.Equal(samples.Length, dataBlock.Data.Length);
            Assert.Equal(samples, dataBlock.Data);
            Assert.Equal(1, dataBlock.StartIndex);
            Assert.Equal(1, dataBlock.EndIndex);
            Assert.Equal(0, dataBlock.BufferPosition);
            Assert.Null(receivedEventArgs.Error);
            Assert.Equal(DefaultBlockSize, buffer.Position);
            Assert.Equal(DefaultBlockSize, buffer.TotalWrites);

            double expectedDurationMs = DefaultBlockSize * (1000.0 / DefaultSamplerate);
            Assert.True(dataBlock.EndTime > dataBlock.StartTime);
            Assert.InRange((dataBlock.EndTime - dataBlock.StartTime).TotalMilliseconds, expectedDurationMs * 0.9, expectedDurationMs * 1.5);
        }

        [Fact]
        public void Write_FillBufferExactly_RaisesEventsAndUpdatesState()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            var blocksToWriteCount = DefaultBufferSize / DefaultBlockSize;

            List<DataBlock> receivedDataBlocks = new List<DataBlock>();
            buffer.BlockReceived += (sender, args) => {
                receivedDataBlocks.Add(args.DataBlock);
            };

            for (int i = 0; i < blocksToWriteCount; i++)
            {
                 buffer.Write(GenerateSampleData(DefaultBlockSize, i * DefaultBlockSize));
            }

            Assert.Equal(blocksToWriteCount, receivedDataBlocks.Count);
            for (int i = 0; i < blocksToWriteCount; i++)
            {
                Assert.Equal(i + 1, receivedDataBlocks[i].StartIndex);
                Assert.Equal(i * DefaultBlockSize, receivedDataBlocks[i].BufferPosition); // Buffer position for these blocks
                Assert.Equal(GenerateSampleData(DefaultBlockSize, i * DefaultBlockSize), receivedDataBlocks[i].Data);
            }
            Assert.Equal(0, buffer.Position);
            Assert.Equal(DefaultBufferSize, buffer.TotalWrites);
        }

        [Fact]
        public void Write_WrapAroundBuffer_OverwritesOldDataAndUpdatesState()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            var blocksToFillAndWrap = (DefaultBufferSize / DefaultBlockSize) + 1;

            List<DataBlock> receivedDataBlocks = new List<DataBlock>();
            buffer.BlockReceived += (sender, args) => {
                receivedDataBlocks.Add(args.DataBlock);
            };

            for(int i = 0; i < blocksToFillAndWrap; i++)
            {
                buffer.Write(GenerateSampleData(DefaultBlockSize, i * 100));
            }

            Assert.Equal(blocksToFillAndWrap, receivedDataBlocks.Count);
            Assert.Equal(DefaultBlockSize, buffer.Position);
            Assert.Equal(blocksToFillAndWrap * DefaultBlockSize, buffer.TotalWrites);

            var lastReceivedDataBlock = receivedDataBlocks.Last();
            Assert.Equal(blocksToFillAndWrap, lastReceivedDataBlock.StartIndex);
            Assert.Equal(0, lastReceivedDataBlock.BufferPosition);
            Assert.Equal(GenerateSampleData(DefaultBlockSize, (blocksToFillAndWrap - 1) * 100), lastReceivedDataBlock.Data);

            // GetBlocks now returns a single DataBlock merging requested blocks
            var currentBufferMergedBlock = buffer.GetBlocks(DefaultBufferSize / DefaultBlockSize);
            // The data in this merged block should represent the current content of the circular buffer.
            // First actual block (data from index 1*100), then (data from 2*100), then (data from 3*100)
            var expectedMergedData = new List<float>();
            // The first block (index 0*100) was overwritten by block index 3*100 (if DefaultBufferSize/DefaultBlockSize is 3)
            for(int i = 1; i < blocksToFillAndWrap; i++) // block 0 was overwritten
            {
                expectedMergedData.AddRange(GenerateSampleData(DefaultBlockSize, i * 100));
            }
            Assert.Equal(expectedMergedData.ToArray(), currentBufferMergedBlock.Data);
        }

        [Fact]
        public void Write_PartialChunksFormingBlock_RaisesBlockReceived()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            var samples = GenerateSampleData(DefaultBlockSize);
            BlockReceivedEventArgs? receivedEventArgs = null;
            int eventCount = 0;

            buffer.BlockReceived += (sender, args) => {
                receivedEventArgs = args;
                eventCount++;
            };

            buffer.Write(samples.Take(DefaultBlockSize / 2).ToArray());
            Assert.Equal(0, eventCount);
            Assert.Equal(DefaultBlockSize / 2, buffer.Position);
            Assert.Equal(DefaultBlockSize / 2, buffer.TotalWrites);

            buffer.Write(samples.Skip(DefaultBlockSize / 2).ToArray());
            Assert.Equal(1, eventCount);
            Assert.NotNull(receivedEventArgs);
            Assert.NotNull(receivedEventArgs.DataBlock);
            Assert.Equal(samples, receivedEventArgs.DataBlock.Data);
            Assert.Equal(1, receivedEventArgs.DataBlock.StartIndex);
            Assert.Equal(DefaultBlockSize, buffer.Position);
            Assert.Equal(DefaultBlockSize, buffer.TotalWrites);
        }

        [Fact]
        public void Write_MultipleBlocksInSingleWrite_RaisesEventsCorrectly()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            var samples = GenerateSampleData(DefaultBlockSize * 2, 50);

            List<DataBlock> receivedDataBlocks = new List<DataBlock>();
            buffer.BlockReceived += (sender, args) => {
                receivedDataBlocks.Add(args.DataBlock);
            };

            buffer.Write(samples);

            Assert.Equal(2, receivedDataBlocks.Count);
            Assert.Equal(GenerateSampleData(DefaultBlockSize, 50), receivedDataBlocks[0].Data);
            Assert.Equal(1, receivedDataBlocks[0].StartIndex);
            Assert.Equal(0, receivedDataBlocks[0].BufferPosition);

            Assert.Equal(GenerateSampleData(DefaultBlockSize, 50 + DefaultBlockSize), receivedDataBlocks[1].Data);
            Assert.Equal(2, receivedDataBlocks[1].StartIndex);
            Assert.Equal(DefaultBlockSize, receivedDataBlocks[1].BufferPosition);

            Assert.Equal(DefaultBlockSize * 2, buffer.Position);
            Assert.Equal(DefaultBlockSize * 2, buffer.TotalWrites);
        }

        [Fact]
        public void GetBlocks_RetrieveSpecificCount_ReturnsCorrectMergedData()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.Write(GenerateSampleData(DefaultBlockSize, 0));    // Block 1
            buffer.Write(GenerateSampleData(DefaultBlockSize, 100));  // Block 2

            // Get 1 block (latest one)
            var mergedBlock = buffer.GetBlocks(1);
            Assert.Equal(GenerateSampleData(DefaultBlockSize, 100), mergedBlock.Data);
            Assert.Equal(2, mergedBlock.StartIndex); // Index of the first block in this merged data
            Assert.Equal(2, mergedBlock.EndIndex);   // Index of the last block in this merged data

            // Get 2 blocks
            mergedBlock = buffer.GetBlocks(2);
            var expectedData = GenerateSampleData(DefaultBlockSize, 0).Concat(GenerateSampleData(DefaultBlockSize, 100)).ToArray();
            Assert.Equal(expectedData, mergedBlock.Data);
            Assert.Equal(1, mergedBlock.StartIndex);
            Assert.Equal(2, mergedBlock.EndIndex);
        }

        [Fact]
        public void GetBlocks_RequestMoreThanAvailable_ReturnsAllAvailableMerged()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.Write(GenerateSampleData(DefaultBlockSize, 0)); // Block 1

            var mergedBlock = buffer.GetBlocks(5); // Request 5 blocks worth, only 1 available
            Assert.Equal(GenerateSampleData(DefaultBlockSize, 0), mergedBlock.Data);
            Assert.Equal(1, mergedBlock.StartIndex);
            Assert.Equal(1, mergedBlock.EndIndex);
        }

        [Fact]
        public void GetBlocks_RetrieveByEndBlockIndex_ReturnsCorrectMergedData()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.Write(GenerateSampleData(DefaultBlockSize, 0));    // Block 1
            buffer.Write(GenerateSampleData(DefaultBlockSize, 100));  // Block 2
            buffer.Write(GenerateSampleData(DefaultBlockSize, 200));  // Block 3

            var mergedBlock = buffer.GetBlocks(2, 2); // Get 2 blocks ending at index 2 (i.e., blocks 1 and 2)
            var expectedData = GenerateSampleData(DefaultBlockSize, 0).Concat(GenerateSampleData(DefaultBlockSize, 100)).ToArray();
            Assert.Equal(expectedData, mergedBlock.Data);
            Assert.Equal(1, mergedBlock.StartIndex);
            Assert.Equal(2, mergedBlock.EndIndex);
        }

        [Fact]
        public void GetBlocks_EmptyRepo_ReturnsEmptyDataBlock()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            var mergedBlock = buffer.GetBlocks(1);
            Assert.Empty(mergedBlock.Data);
            Assert.Equal(0, mergedBlock.EndIndex); // Based on ShiftingBuffer's empty case
            Assert.Equal(1, mergedBlock.StartIndex); // Based on ShiftingBuffer's empty case (endIndex - blockCount + 1)
        }

        [Fact]
        public void Clear_ResetsBufferStateAndRepo()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.Write(GenerateSampleData(DefaultBlockSize * 2));
            buffer.Clear();

            Assert.Equal(0, buffer.Position);
            Assert.Equal(0, buffer.TotalWrites);
            var mergedBlock = buffer.GetBlocks(1);
            Assert.Empty(mergedBlock.Data);
        }

        [Fact]
        public void Error_ClearBufferMode_ClearsBufferAndRaisesEvent()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.ErrorHandlingMode = BufferErrorHandlingMode.ClearBuffer;
            buffer.Write(GenerateSampleData(DefaultBlockSize));

            BufferErrorEventArgs? errorArgs = null;
            buffer.BufferError += (sender, args) => {
                errorArgs = args;
            };

            long initialTotalWrites = buffer.TotalWrites;
            // Error signature: Error(int bytesAvailable, int bytesLost, int bytesCorrupted, int bytesTotal)
            buffer.Error(bytesAvailable: 0, bytesLost: 5, bytesCorrupted: 0, bytesTotal: DefaultBlockSize);

            Assert.NotNull(errorArgs);
            Assert.Equal(5, errorArgs.BytesLost);
            Assert.Equal(0, errorArgs.BytesCorrupted);
            Assert.Equal(DefaultBlockSize, errorArgs.BytesTotal);

            Assert.Equal(0, buffer.Position);
            Assert.Equal(0, buffer.TotalWrites); // TotalWrites is reset by Clear
            Assert.Empty(buffer.GetBlocks(1).Data);
        }

        [Fact]
        public void Error_DiscardSamplesMode_RaisesEventDoesNotClearImmediately()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.ErrorHandlingMode = BufferErrorHandlingMode.DiscardSamples;
            buffer.Write(GenerateSampleData(DefaultBlockSize / 2));

            BufferErrorEventArgs? errorArgs = null;
            buffer.BufferError += (sender, args) => {
                errorArgs = args;
            };

            long initialPosition = buffer.Position;
            long initialTotalWrites = buffer.TotalWrites;

            buffer.Error(bytesAvailable: DefaultBlockSize / 2, bytesLost: 5, bytesCorrupted: 0, bytesTotal: 5);

            Assert.NotNull(errorArgs);
            Assert.Equal(5, errorArgs.BytesLost);
            Assert.Equal(initialPosition, buffer.Position);
            Assert.Equal(initialTotalWrites, buffer.TotalWrites);

            BlockReceivedEventArgs? blockArgs = null;
            buffer.BlockReceived += (s,a) => blockArgs = a;
            buffer.Write(GenerateSampleData(DefaultBlockSize / 2, DefaultBlockSize/2));

            Assert.NotNull(blockArgs); // Block should still be formed
            Assert.NotNull(blockArgs.Error); // Error should be attached
            Assert.Equal(5, blockArgs.Error.LostBytes);
        }

        [Fact]
        public void Error_ZeroSamplesMode_ZerosAffectedRegionAndRaisesEvent()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            buffer.ErrorHandlingMode = BufferErrorHandlingMode.ZeroSamples;

            BufferErrorEventArgs? bufferErrorArgs = null;
            buffer.BufferError += (sender, args) => {
                bufferErrorArgs = args;
            };

            BlockReceivedEventArgs? blockEventArgs = null;
            var mre = new ManualResetEvent(false);

            buffer.BlockReceived += (sender, args) => {
                blockEventArgs = args;
                mre.Set();
            };

            // Write first part of a block that will be formed
            var part1 = GenerateSampleData(DefaultBlockSize / 2, 1); // 1,2,3,4,5
            buffer.Write(part1);

            // Report an error that affects the *next* data to be written.
            // Let's say 2 samples are corrupted, 0 lost, out of an expected segment of 2.
            // The Error() call in ZeroSamples mode writes zeros for 'bytesTotal'. This is a bit confusing.
            // Based on implementation: Error() with ZeroSamples writes 'bytesTotal' zeros.
            // The arguments bytesLost/Corrupted primarily populate the BlockError object.
            int corruptedCount = 2;
            buffer.Error(bytesAvailable: 0, bytesLost: 0, bytesCorrupted: corruptedCount, bytesTotal: corruptedCount);
            Assert.NotNull(bufferErrorArgs);
            Assert.Equal(corruptedCount, bufferErrorArgs.BytesCorrupted);
            Assert.Equal(corruptedCount, bufferErrorArgs.BytesTotal); // Error was for a 2-byte segment

            // Write the "rest" of the samples that would complete the block
            var part2 = GenerateSampleData(DefaultBlockSize / 2 - corruptedCount, 6); // 6,7,8 (if DefaultBlockSize=10, corrupted=2)
            buffer.Write(part2);

            mre.WaitOne(TimeSpan.FromSeconds(1));
            Assert.NotNull(blockEventArgs);
            Assert.NotNull(blockEventArgs.DataBlock);
            Assert.NotNull(blockEventArgs.Error);
            Assert.Equal(corruptedCount, blockEventArgs.Error.CorruptedBytes);

            var expectedData = new List<float>();
            expectedData.AddRange(part1); // 1,2,3,4,5
            expectedData.AddRange(new float[corruptedCount]); // 0,0
            expectedData.AddRange(part2); // 6,7,8
            Assert.Equal(expectedData.ToArray(), blockEventArgs.DataBlock.Data);
        }

        [Fact]
        public void Error_MultipleCalls_AccumulatesErrorInLastErrorAndResetsOnBlockReceived()
        {
            var buffer = new ShiftingBuffer(DefaultBufferSize, DefaultBlockSize, DefaultSamplerate);
            // No general BufferError handler for this test initially, to simplify analysis of Error 3.

            // Error 1 & Error 2 sequence (checking accumulation in BlockError via BlockReceived)
            BlockError? errorFromBlockEvent = null; // To store BlockError from BlockReceivedEventArgs
            BufferErrorEventArgs? capturedBufferErrorArgs = null;
            int generalErrorEventCount = 0;

            ShiftingBuffer.BufferErrorEventHandler generalHandler = (sender, args) => {
                capturedBufferErrorArgs = args;
                generalErrorEventCount++;
            };
            buffer.BufferError += generalHandler;

            // Error 1
            buffer.Error(bytesAvailable:0, bytesLost:1, bytesCorrupted:1, bytesTotal:2);
            Assert.Equal(1, generalErrorEventCount);
            Assert.NotNull(capturedBufferErrorArgs);

            // Error 2
            buffer.Error(bytesAvailable:0, bytesLost:2, bytesCorrupted:0, bytesTotal:2);
            Assert.Equal(2, generalErrorEventCount);

            BlockReceivedEventArgs? blockEventArgs = null;
            // Using a fresh handler for BlockReceived to avoid interactions if any existed.
            ShiftingBuffer.BlockReceivedEventHandler blockReceivedHandler = (s, a) => {
                blockEventArgs = a;
                errorFromBlockEvent = a.Error; // Capture the error state passed with the block
            };
            buffer.BlockReceived += blockReceivedHandler;

            buffer.Write(GenerateSampleData(DefaultBlockSize));

            Assert.NotNull(blockEventArgs);
            Assert.NotNull(errorFromBlockEvent);
            Assert.Equal(2, errorFromBlockEvent.ErrorCount);
            Assert.Equal(1 + 2, errorFromBlockEvent.LostBytes);
            Assert.Equal(1 + 0, errorFromBlockEvent.CorruptedBytes);

            buffer.BlockReceived -= blockReceivedHandler; // Clean up this handler
            buffer.BufferError -= generalHandler; // Clean up general handler before Error 3 test

            // After a block is successfully received, the internal _lastError should be reset.
            // Now test Error 3 with a dedicated handler
            object? capturedArgsObj = null; // Use object type for capture
            int error3EventCount = 0;
            bool error3HandlerLogicExecuted = false;

            ShiftingBuffer.BufferErrorEventHandler specificError3Handler = (sender, args) => {
                if (args == null)
                {
                    // This exception will fail the test immediately if args is null.
                    throw new InvalidOperationException("BufferErrorEventArgs (args) is unexpectedly null in event handler for Error 3.");
                }
                capturedArgsObj = args; // Assign to object
                error3HandlerLogicExecuted = true;
                error3EventCount++;
            };
            buffer.BufferError += specificError3Handler;

            buffer.Error(bytesAvailable:0, bytesLost:1, bytesCorrupted:0, bytesTotal:1); // This is Error 3

            Assert.True(error3HandlerLogicExecuted, "BufferError event handler logic for Error 3 was not executed as expected.");
            Assert.Equal(1, error3EventCount);
            Assert.NotNull(capturedArgsObj);  // Check object
            BufferErrorEventArgs? error3EventArgs = capturedArgsObj as BufferErrorEventArgs; // Cast
            Assert.NotNull(error3EventArgs); // Check casted object
            Assert.Equal(1, error3EventArgs.BytesLost);
            Assert.Equal(0, error3EventArgs.BytesCorrupted);

            buffer.BufferError -= specificError3Handler; // Clean up specific handler

            // Verify this new _lastError (created by Error 3) is attached to the *next* received block
            blockEventArgs = null;
            errorFromBlockEvent = null; // Reset for clarity
            // Re-subscribe a handler for the next BlockReceived event
            // It's better to use a new handler variable if the old one might be captured with stale context,
            // but for this simple lambda, re-adding is fine.
            buffer.BlockReceived += blockReceivedHandler; // Re-using the same handler logic, effectively re-subscribing

            buffer.Write(GenerateSampleData(DefaultBlockSize, 100));
            Assert.NotNull(blockEventArgs);
            Assert.NotNull(errorFromBlockEvent);
            Assert.Equal(1, errorFromBlockEvent.ErrorCount);
            Assert.Equal(1, errorFromBlockEvent.LostBytes);
            Assert.Equal(0, errorFromBlockEvent.CorruptedBytes);

            buffer.BlockReceived -= blockReceivedHandler; // Final cleanup
        }
    }
}