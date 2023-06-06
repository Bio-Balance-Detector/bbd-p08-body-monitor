using System.Runtime.InteropServices;

namespace BBD.BodyMonitor.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WavePcmFormat
    {
        /* ChunkID          Contains the letters "RIFF" in ASCII form */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private char[] chunkID = new char[] { 'R', 'I', 'F', 'F' };

        /* ChunkSize        36 + SubChunk2Size */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        private uint chunkSize = 0;

        /* Format           The "WAVE" format name */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private char[] format = new char[] { 'W', 'A', 'V', 'E' };

        /* Subchunk1ID      Contains the letters "fmt " */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private char[] subchunk1ID = new char[] { 'f', 'm', 't', ' ' };

        /* Subchunk1Size    16 for PCM */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        private uint subchunk1Size = 16;

        /* AudioFormat      PCM = 1 (i.e. Linear quantization) */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        private ushort audioFormat = 1;

        /* NumChannels      Mono = 1, Stereo = 2, etc. */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        private ushort numChannels = 1;
        public ushort NumChannels { get => numChannels; set => numChannels = value; }

        /* SampleRate       8000, 44100, etc. */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        private uint sampleRate = 44100;
        public uint SampleRate { get => sampleRate; set => sampleRate = value; }

        /* ByteRate         == SampleRate * NumChannels * BitsPerSample/8 */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        private uint byteRate = 0;

        /* BlockAlign       == NumChannels * BitsPerSample/8 */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        private ushort blockAlign = 0;

        /* BitsPerSample    8 bits = 8, 16 bits = 16, etc. */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        private ushort bitsPerSample = 16;
        public ushort BitsPerSample { get => bitsPerSample; set => bitsPerSample = value; }

        /* Subchunk2ID      Contains the letters "data" */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private char[] subchunk2ID = new char[] { 'd', 'a', 't', 'a' };

        /* Subchunk2Size    == NumSamples * NumChannels * BitsPerSample/8 */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        private uint subchunk2Size = 0;

        public uint DataChuckSize => subchunk2Size;

        public uint DataChuckPosition
        {
            get
            {
                return 44;
            }
        }

        /* Data             The actual sound data. */
        public byte[] Data { get; set; } = new byte[0];
        public byte BytesPerSample
        {
            get
            {
                if (this.BitsPerSample % 8 != 0)
                {
                    throw new Exception("Only 8, 16, 24 and 32-bit WAV files are supported.");
                }

                return (byte)(this.BitsPerSample / 8);
            }
        }

        public WavePcmFormat() { }
        public WavePcmFormat(byte[] data, ushort numChannels = 2, uint sampleRate = 44100, ushort bitsPerSample = 16)
        {
            Data = data;
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

        public byte[] ToByteArray()
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

        public static WavePcmFormat FromByteArray(byte[] header)
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

            return waveHeader;
        }
    }
}
