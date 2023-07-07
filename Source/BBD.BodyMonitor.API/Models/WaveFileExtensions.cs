using NWaves.Audio;
using NWaves.Signals;

namespace BBD.BodyMonitor.Models
{
    public static class WaveFileExtensions
    {
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

            if (waveFileStream.Length < 44)
            {
                WaveFile wf = new(signalToSave, 16);
                wf.SaveTo(waveFileStream, false);
                return;
            }
            else
            {
                WavePcmFormatHeader waveHeader = WaveFileExtensions.ReadWaveFileHeader(waveFileStream);

                if (waveHeader.NumChannels != 1)
                {
                    throw new Exception("Only mono WAV files are supported.");
                }

                if (waveHeader.SampleRate != signalToSave.SamplingRate)
                {
                    throw new Exception("The sampling rate of the WAV file does not match the sampling rate of the signal.");
                }

                if (waveHeader.BitsPerSample != 16)
                {
                    throw new Exception("Only 16-bit WAV files are supported.");
                }

                uint dataChunkSize = (uint)(waveFileStream.Length - 44);

                _ = waveFileStream.Seek(0, SeekOrigin.End);

                byte[] dataToAppend = signalToSave.Samples.SelectMany(s => BitConverter.GetBytes((short)s)).ToArray();

                waveFileStream.Write(dataToAppend);

                // update data chunk size
                _ = waveFileStream.Seek(40, SeekOrigin.Begin);
                waveFileStream.Write(BitConverter.GetBytes(dataChunkSize + (uint)dataToAppend.Length));
            }
        }

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
                throw new Exception("Cannot read from the file stream.");
            }

            _ = waveStream.Seek(0, SeekOrigin.Begin);

            // read wave file header
            byte[] headerBytes = new byte[44];
            _ = waveStream.Read(headerBytes, 0, 44);

            return WavePcmFormatHeader.FromByteArray(headerBytes);
        }

        public static DiscreteSignal ReadAsDiscreateSignal(Stream waveStream, double position, float interval)
        {
            WavePcmFormatHeader waveHeader = ReadWaveFileHeader(waveStream);

            if (waveHeader.NumChannels != 1)
            {
                throw new Exception("Only mono WAV files are supported.");
            }

            uint readPositionStart = (uint)(waveHeader.SampleRate * waveHeader.BytesPerSample * position);
            if (readPositionStart % waveHeader.BytesPerSample != 0)
            {
                readPositionStart -= (readPositionStart % waveHeader.BytesPerSample);
            }
            readPositionStart += waveHeader.DataChuckPosition;

            int numberOfSamplesToRead = (int)(interval * waveHeader.SampleRate);

            byte[] buffer = new byte[numberOfSamplesToRead * waveHeader.BytesPerSample];
            _ = waveStream.Seek(readPositionStart, SeekOrigin.Begin);
            _ = waveStream.Read(buffer);

            short[] samplesInt16 = new short[numberOfSamplesToRead];
            Buffer.BlockCopy(buffer, 0, samplesInt16, 0, buffer.Length);

            float[] samplesFloat32 = Array.ConvertAll<short, float>(samplesInt16, s => (float)s / short.MaxValue);
            return new DiscreteSignal((int)waveHeader.SampleRate, samplesFloat32);
        }
    }
}
