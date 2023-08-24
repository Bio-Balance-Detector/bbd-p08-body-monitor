using System.Runtime.InteropServices;

namespace BBD.BodyMonitor.Models
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class WavePcmFormatHeader
    {
        /* ChunkID          Contains the letters "RIFF" in ASCII form */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] chunkID = new char[] { 'R', 'I', 'F', 'F' };

        /* ChunkSize        36 + SubChunk2Size */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint chunkSize = 0;

        /* Format           The "WAVE" format name */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] format = new char[] { 'W', 'A', 'V', 'E' };

        /* Subchunk1ID      Contains the letters "fmt " */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] subchunk1ID = new char[] { 'f', 'm', 't', ' ' };

        /* Subchunk1Size    16 for PCM */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected readonly uint subchunk1Size = 16;

        /* AudioFormat      PCM = 1 (i.e. Linear quantization) */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected readonly ushort audioFormat = 1;

        /* NumChannels      Mono = 1, Stereo = 2, etc. */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected ushort numChannels = 1;
        public ushort NumChannels { get => numChannels; set => numChannels = value; }

        /* SampleRate       8000, 44100, etc. */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint sampleRate = 44100;
        public uint SampleRate { get => sampleRate; set => sampleRate = value; }

        /* ByteRate         == SampleRate * NumChannels * BitsPerSample/8 */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint byteRate = 0;

        /* BlockAlign       == NumChannels * BitsPerSample/8 */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected ushort blockAlign = 0;

        /* BitsPerSample    8 bits = 8, 16 bits = 16, etc. */
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected ushort bitsPerSample = 16;
        public ushort BitsPerSample { get => bitsPerSample; set => bitsPerSample = value; }

        /* Subchunk2ID      Contains the letters "data" */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] subchunk2ID = new char[] { 'd', 'a', 't', 'a' };

        /* Subchunk2Size    == NumSamples * NumChannels * BitsPerSample/8 */
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint subchunk2Size = 0;

        public uint DataChuckSize => subchunk2Size;

        public uint DataChuckPosition => 44;

        public byte BytesPerSample => BitsPerSample % 8 != 0
                    ? throw new Exception("Only 8, 16, 24 and 32-bit WAV files are supported.")
                    : (byte)(BitsPerSample / 8);

        /// <summary>
        /// This constructor is required for dynamic object creation during deserialization.
        /// </summary>
        public WavePcmFormatHeader() { }

        public WavePcmFormatHeader(ushort numChannels = 2, uint sampleRate = 44100, ushort bitsPerSample = 16)
        {
            NumChannels = numChannels;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
        }

        public virtual byte[] ToByteArray()
        {
            int headerSize = Marshal.SizeOf(this);
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            Marshal.StructureToPtr(this, headerPtr, false);
            byte[] rawData = new byte[headerSize];
            Marshal.Copy(headerPtr, rawData, 0, headerSize);
            Marshal.FreeHGlobal(headerPtr);
            return rawData;
        }

        public static WavePcmFormatHeader FromByteArray(byte[] header)
        {
            WavePcmFormatHeader waveHeader;

            GCHandle handle = GCHandle.Alloc(header, GCHandleType.Pinned);
            try
            {
                waveHeader = (WavePcmFormatHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(WavePcmFormatHeader));
            }
            finally
            {
                handle.Free();
            }

            return waveHeader.chunkID[0] != 'R' || waveHeader.chunkID[1] != 'I' || waveHeader.chunkID[2] != 'F' || waveHeader.chunkID[3] != 'F'
                ? throw new System.Exception("Invalid WAV file")
                : waveHeader.format[0] != 'W' || waveHeader.format[1] != 'A' || waveHeader.format[2] != 'V' || waveHeader.format[3] != 'E'
                ? throw new System.Exception("Only 'WAVE' format is supported.")
                : waveHeader.subchunk1ID[0] != 'f' || waveHeader.subchunk1ID[1] != 'm' || waveHeader.subchunk1ID[2] != 't' || waveHeader.subchunk1ID[3] != ' '
                ? throw new System.Exception("The first chunk in the WAV file must be 'fmt '.")
                : waveHeader.subchunk2ID[0] != 'd' || waveHeader.subchunk2ID[1] != 'a' || waveHeader.subchunk2ID[2] != 't' || waveHeader.subchunk2ID[3] != 'a'
                ? throw new System.Exception("The second chunk in the WAV file must be 'data'.")
                : waveHeader;
        }
    }
}
