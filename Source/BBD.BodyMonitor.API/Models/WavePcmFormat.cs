using System.Runtime.InteropServices;

namespace BBD.BodyMonitor.Models
{
    public class WavePcmFormat : WavePcmFormatHeader
    {
        /* Data             The actual sound data. */
        public byte[] Data { get; set; } = new byte[0];

        /// <summary>
        /// This constructor is required for dynamic object creation during deserialization.
        /// </summary>
        public WavePcmFormat() { }

        public WavePcmFormat(ushort numChannels = 2, uint sampleRate = 44100, ushort bitsPerSample = 16, byte[]? data = null)
        {
            Data = data ?? new byte[0];
            NumChannels = numChannels;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
        }

        private void CalculateSizes()
        {
            subchunk2Size = (uint)Data.Length;
            blockAlign = (ushort)(NumChannels * BitsPerSample / 8);
            byteRate = SampleRate * NumChannels * BitsPerSample / 8;
            chunkSize = 36 + subchunk2Size;
        }

        public override byte[] ToByteArray()
        {
            CalculateSizes();
            int headerSize = Marshal.SizeOf(this);
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            Marshal.StructureToPtr(this, headerPtr, false);
            byte[] rawData = new byte[headerSize + Data.Length];
            Marshal.Copy(headerPtr, rawData, 0, headerSize);
            Marshal.FreeHGlobal(headerPtr);
            Array.Copy(Data, 0, rawData, 44, Data.Length);
            return rawData;
        }

        public static new WavePcmFormat FromByteArray(byte[] header)
        {
            WavePcmFormat waveHeader;

            GCHandle handle = GCHandle.Alloc(header, GCHandleType.Pinned);
            try
            {
                waveHeader = (WavePcmFormat)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(WavePcmFormat));
            }
            finally
            {
                handle.Free();
            }

            if (waveHeader.chunkID[0] != 'R' || waveHeader.chunkID[1] != 'I' || waveHeader.chunkID[2] != 'F' || waveHeader.chunkID[3] != 'F')
            {
                throw new System.Exception("Invalid WAV file");
            }

            if (waveHeader.format[0] != 'W' || waveHeader.format[1] != 'A' || waveHeader.format[2] != 'V' || waveHeader.format[3] != 'E')
            {
                throw new System.Exception("Only 'WAVE' format is supported.");
            }

            if (waveHeader.subchunk1ID[0] != 'f' || waveHeader.subchunk1ID[1] != 'm' || waveHeader.subchunk1ID[2] != 't' || waveHeader.subchunk1ID[3] != ' ')
            {
                throw new System.Exception("The first chunk in the WAV file must be 'fmt '.");
            }

            if (waveHeader.subchunk2ID[0] != 'd' || waveHeader.subchunk2ID[1] != 'a' || waveHeader.subchunk2ID[2] != 't' || waveHeader.subchunk2ID[3] != 'a')
            {
                throw new System.Exception("The second chunk in the WAV file must be 'data'.");
            }

            waveHeader.CalculateSizes();

            return waveHeader;
        }
    }
}
