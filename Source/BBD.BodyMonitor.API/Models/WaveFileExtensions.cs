using NWaves.Audio;
using NWaves.Signals;

namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Provides extension methods for handling Wave audio files, particularly for operations like appending data and reading headers or segments.
    /// </summary>
    public static class WaveFileExtensions
    {
        /// <summary>
        /// Appends a <see cref="DiscreteSignal"/> to an existing mono, 16-bit PCM WAV file.
        /// If the file does not exist or is not a valid WAV file, a new WAV file is created with the signal data.
        /// The method updates the WAV file header to reflect the new data size.
        /// </summary>
        /// <param name="waveFileStream">The <see cref="FileStream"/> of the WAV file to append to. Must be readable, seekable, and writable.</param>
        /// <param name="signalToSave">The <see cref="DiscreteSignal"/> containing the audio data to append. The signal's sampling rate must match the WAV file's sampling rate.</param>
        /// <exception cref="Exception">Thrown if the file stream does not support read, seek, or write operations;
        /// if the existing WAV file is not mono or not 16-bit PCM;
        /// or if the signal's sampling rate does not match the WAV file's sampling rate.</exception>
        public static void AppendTo(FileStream waveFileStream, DiscreteSignal signalToSave)
        {
            if (waveFileStream.CanRead == false)
            {
                throw new Exception("Cannot read from the file stream.");
            }

            if (waveFileStream.CanSeek == false)
            {
                throw new Exception("Cannot seek in the file stream.");
            }

            if (waveFileStream.CanWrite == false)
            {
                throw new Exception("Cannot write in the file stream.");
            }

            if (waveFileStream.Length < 44) // Standard WAV header size
            {
                // File is too small to be a valid WAV or doesn't exist, create a new one.
                WaveFile wf = new(signalToSave, 16); // Assuming 16-bit depth for new files
                wf.SaveTo(waveFileStream, false); // `false` for not leaving the stream open, assuming WaveFile handles it or it's managed outside.
                return;
            }
            else
            {
                WavePcmFormatHeader waveHeader = WaveFileExtensions.ReadWaveFileHeader(waveFileStream);

                if (waveHeader.NumChannels != 1)
                {
                    throw new Exception("Only mono WAV files are supported for appending.");
                }

                if (waveHeader.SampleRate != signalToSave.SamplingRate)
                {
                    throw new Exception($"The sampling rate of the WAV file ({waveHeader.SampleRate} Hz) does not match the sampling rate of the signal ({signalToSave.SamplingRate} Hz).");
                }

                if (waveHeader.BitsPerSample != 16)
                {
                    throw new Exception("Only 16-bit WAV files are supported for appending.");
                }

                // The current size of the data chunk, before appending.
                // This assumes the 'data' chunk is the last chunk and its size is at offset 40.
                uint dataChunkSize = (uint)(waveFileStream.Length - waveHeader.DataChuckPosition); // Correctly calculate current data chunk size based on header info if available, otherwise from total length.
                                                                                              // For a simple WAV file, data starts at byte 44. If DataChuckPosition is reliable, use it.
                                                                                              // Assuming DataChuckPosition is 44 for standard files.
                if(waveHeader.DataChuckPosition == 0) // Fallback if DataChuckPosition wasn't parsed properly or is 0
                {
                     dataChunkSize = (uint)(waveFileStream.Length - 44);
                }


                _ = waveFileStream.Seek(0, SeekOrigin.End);

                // Convert float samples to short (16-bit) and then to bytes
                byte[] dataToAppend = signalToSave.Samples.SelectMany(s => BitConverter.GetBytes((short)(s * short.MaxValue))).ToArray();

                waveFileStream.Write(dataToAppend, 0, dataToAppend.Length); // Explicitly pass offset and count

                // update RIFF chunk size (overall file size - 8 bytes)
                uint newRiffChunkSize = (uint)waveFileStream.Length - 8;
                _ = waveFileStream.Seek(4, SeekOrigin.Begin);
                waveFileStream.Write(BitConverter.GetBytes(newRiffChunkSize), 0, 4);

                // update data chunk size in 'data' sub-chunk header
                uint newDataChunkSize = dataChunkSize + (uint)dataToAppend.Length;
                _ = waveFileStream.Seek((int)waveHeader.DataChuckPosition - 4, SeekOrigin.Begin); // Position to 'data' chunk size field (assuming 'data' marker is 4 bytes before size)
                                                                                                  // This assumes DataChuckPosition points to the start of sample data, so size is 4 bytes before.
                                                                                                  // Standard WAV: 'data' at 36, size at 40. So if DataChuckPosition = 44, this is correct.
                waveFileStream.Write(BitConverter.GetBytes(newDataChunkSize), 0, 4);
            }
        }

        /// <summary>
        /// Reads the header of a WAV file from a stream.
        /// </summary>
        /// <param name="waveStream">The stream containing the WAV file data. Must be readable and seekable, and contain at least 44 bytes.</param>
        /// <returns>A <see cref="WavePcmFormatHeader"/> object populated with data from the WAV file header.</returns>
        /// <exception cref="Exception">Thrown if the stream does not support read or seek operations, or if the stream length is less than 44 bytes.</exception>
        public static WavePcmFormatHeader ReadWaveFileHeader(Stream waveStream)
        {
            if (waveStream.CanRead == false)
            {
                throw new Exception("Cannot read from the file stream.");
            }

            if (waveStream.CanSeek == false)
            {
                throw new Exception("Cannot seek in the file stream.");
            }

            if (waveStream.Length < 44)
            {
                throw new Exception("Stream length is less than the required 44 bytes for a WAV header.");
            }

            _ = waveStream.Seek(0, SeekOrigin.Begin);

            // read wave file header
            byte[] headerBytes = new byte[44];
            int bytesRead = waveStream.Read(headerBytes, 0, 44);
            if (bytesRead < 44)
            {
                throw new Exception("Failed to read the complete 44-byte WAV header.");
            }

            return WavePcmFormatHeader.FromByteArray(headerBytes);
        }

        /// <summary>
        /// Reads a segment of audio data from a WAV file stream and returns it as a <see cref="DiscreteSignal"/>.
        /// This method assumes the WAV file is mono.
        /// </summary>
        /// <param name="waveStream">The stream containing the WAV file data. Must be readable and seekable.</param>
        /// <param name="position">The starting position in seconds from the beginning of the audio data from which to read.</param>
        /// <param name="interval">The length of the audio segment to read, in seconds.</param>
        /// <returns>A <see cref="DiscreteSignal"/> containing the audio data read from the specified segment. Samples are normalized to floats between -1.0 and 1.0.</returns>
        /// <exception cref="Exception">Thrown if the WAV file is not mono, or if read operations fail.</exception>
        public static DiscreteSignal ReadAsDiscreteSignal(Stream waveStream, double position, float interval)
        {
            WavePcmFormatHeader waveHeader = ReadWaveFileHeader(waveStream);

            if (waveHeader.NumChannels != 1)
            {
                throw new Exception("Only mono WAV files are supported.");
            }

            // Calculate start position in bytes from the beginning of the data chunk
            long readPositionDataStartBytes = (long)(waveHeader.SampleRate * position * (waveHeader.BitsPerSample / 8));

            // Align to sample boundary (block align)
            if (readPositionDataStartBytes % waveHeader.BlockAlign != 0)
            {
                readPositionDataStartBytes -= (readPositionDataStartBytes % waveHeader.BlockAlign);
            }

            // Actual seek position in stream: header size (DataChuckPosition) + offset in data
            long actualStreamSeekPosition = waveHeader.DataChuckPosition + readPositionDataStartBytes;

            int numberOfSamplesToRead = (int)(interval * waveHeader.SampleRate);
            int bytesToRead = numberOfSamplesToRead * (waveHeader.BitsPerSample / 8);

            if (actualStreamSeekPosition + bytesToRead > waveStream.Length)
            {
                bytesToRead = (int)(waveStream.Length - actualStreamSeekPosition);
                numberOfSamplesToRead = bytesToRead / (waveHeader.BitsPerSample / 8);
                if (bytesToRead <= 0) throw new ArgumentOutOfRangeException(nameof(position), "Calculated read position is beyond the end of the stream or results in zero bytes to read.");
            }

            byte[] buffer = new byte[bytesToRead];
            _ = waveStream.Seek(actualStreamSeekPosition, SeekOrigin.Begin);
            int bytesActuallyRead = waveStream.Read(buffer, 0, buffer.Length); // Use offset and count for Read

            // Adjust numberOfSamplesToRead if fewer bytes were actually read than requested (e.g., end of stream)
            numberOfSamplesToRead = bytesActuallyRead / (waveHeader.BitsPerSample / 8);
            if (numberOfSamplesToRead == 0 && bytesActuallyRead > 0 && (waveHeader.BitsPerSample / 8) > bytesActuallyRead)
            {
                 // Partial sample read, not enough for a full sample based on BitsPerSample.
                 // This indicates an issue or truncated file. For simplicity, return empty or handle as error.
                 return new DiscreteSignal((int)waveHeader.SampleRate, Array.Empty<float>());
            }


            short[] samplesInt16 = new short[numberOfSamplesToRead];
            // Ensure we only copy the bytes that were actually read and form complete samples
            Buffer.BlockCopy(buffer, 0, samplesInt16, 0, numberOfSamplesToRead * (waveHeader.BitsPerSample / 8));

            float[] samplesFloat32 = Array.ConvertAll<short, float>(samplesInt16, s => (float)s / short.MaxValue);
            return new DiscreteSignal((int)waveHeader.SampleRate, samplesFloat32);
        }
    }
}
