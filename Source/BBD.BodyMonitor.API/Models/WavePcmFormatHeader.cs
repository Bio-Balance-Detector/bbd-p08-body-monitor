using System.Runtime.InteropServices;

namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Represents the header of a PCM (Pulse Code Modulation) WAV audio file.
    /// This class is designed with a specific memory layout for direct marshaling from/to byte arrays.
    /// It contains metadata about the WAV file format, such as sample rate, number of channels, and bits per sample.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)] // 'Pack = 1' ensures no padding between fields for accurate marshaling.
    public class WavePcmFormatHeader
    {
        /// <summary>
        /// Contains the letters "RIFF" in ASCII form (0x52494646). This indicates a RIFF container.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] chunkID = new char[] { 'R', 'I', 'F', 'F' };

        /// <summary>
        /// Represents the size of the rest of the chunk following this field. This is (file size - 8 bytes).
        /// Calculated as 36 + SubChunk2Size.
        /// </summary>
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint chunkSize = 0;

        /// <summary>
        /// Contains the letters "WAVE" in ASCII form (0x57415645). This identifies the WAV format.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] format = new char[] { 'W', 'A', 'V', 'E' };

        /// <summary>
        /// Contains the letters "fmt " (includes a trailing space) in ASCII form (0x666d7420). This indicates the start of the format sub-chunk.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] subchunk1ID = new char[] { 'f', 'm', 't', ' ' };

        /// <summary>
        /// Represents the size of the rest of the 'fmt ' sub-chunk following this field. For PCM, this is 16.
        /// </summary>
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected readonly uint subchunk1Size = 16;

        /// <summary>
        /// Audio format. For PCM (Linear Quantization), this value is 1. Other values indicate some form of compression.
        /// </summary>
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected readonly ushort audioFormat = 1;

        /// <summary>
        /// Backing field for the number of audio channels.
        /// </summary>
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected ushort numChannels = 1;
        /// <summary>
        /// Gets or sets the number of audio channels (e.g., 1 for mono, 2 for stereo).
        /// </summary>
        public ushort NumChannels { get => numChannels; set => numChannels = value; }

        /// <summary>
        /// Backing field for the sample rate.
        /// </summary>
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint sampleRate = 44100;
        /// <summary>
        /// Gets or sets the sample rate in Hertz (samples per second, e.g., 8000, 44100).
        /// </summary>
        public uint SampleRate { get => sampleRate; set => sampleRate = value; }

        /// <summary>
        /// Byte rate of the audio data. Calculated as SampleRate * NumChannels * BitsPerSample / 8.
        /// </summary>
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint byteRate = 0;

        /// <summary>
        /// Block alignment in bytes. Calculated as NumChannels * BitsPerSample / 8.
        /// This is the number of bytes for one sample including all channels.
        /// </summary>
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected ushort blockAlign = 0;

        /// <summary>
        /// Backing field for bits per sample.
        /// </summary>
        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        protected ushort bitsPerSample = 16;
        /// <summary>
        /// Gets or sets the number of bits per sample (e.g., 8 for 8-bit, 16 for 16-bit).
        /// </summary>
        public ushort BitsPerSample { get => bitsPerSample; set => bitsPerSample = value; }

        /// <summary>
        /// Contains the letters "data" in ASCII form (0x64617461). This indicates the start of the data sub-chunk.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        protected readonly char[] subchunk2ID = new char[] { 'd', 'a', 't', 'a' };

        /// <summary>
        /// Size of the data sub-chunk (the actual sound data). Calculated as NumSamples * NumChannels * BitsPerSample / 8.
        /// </summary>
        [MarshalAs(UnmanagedType.U4, SizeConst = 4)]
        protected uint subchunk2Size = 0;

        /// <summary>
        /// Gets the size of the data chunk (Subchunk2Size) in bytes.
        /// This represents the total number of bytes of actual audio sample data.
        /// </summary>
        public uint DataChunkSize => subchunk2Size; // Corrected typo: DataChuckSize -> DataChunkSize

        /// <summary>
        /// Gets the starting position (offset) of the data chunk within the WAV file, in bytes.
        /// For a standard PCM WAV file without extra chunks, this is typically 44 bytes from the beginning of the file.
        /// </summary>
        public uint DataChunkPosition => 44; // Corrected typo: DataChuckPosition -> DataChunkPosition

        /// <summary>
        /// Gets the number of bytes per sample for a single channel.
        /// </summary>
        /// <exception cref="System.Exception">Thrown if BitsPerSample is not a multiple of 8 (e.g., not 8, 16, 24, or 32).</exception>
        public byte BytesPerSample => BitsPerSample % 8 != 0
                    ? throw new Exception("Only 8, 16, 24 and 32-bit WAV files are supported (BitsPerSample must be a multiple of 8).")
                    : (byte)(BitsPerSample / 8);

        /// <summary>
        /// Initializes a new instance of the <see cref="WavePcmFormatHeader"/> class with default PCM values.
        /// This constructor is also used for dynamic object creation during deserialization or marshaling.
        /// </summary>
        public WavePcmFormatHeader() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WavePcmFormatHeader"/> class with specified audio parameters.
        /// Other fields like <see cref="byteRate"/> and <see cref="blockAlign"/> should be calculated separately if needed before serialization.
        /// </summary>
        /// <param name="numChannels">The number of audio channels (e.g., 1 for mono, 2 for stereo). Default is 2.</param>
        /// <param name="sampleRate">The sample rate in Hertz (e.g., 44100). Default is 44100 Hz.</param>
        /// <param name="bitsPerSample">The number of bits per sample (e.g., 16). Default is 16.</param>
        public WavePcmFormatHeader(ushort numChannels = 2, uint sampleRate = 44100, ushort bitsPerSample = 16)
        {
            NumChannels = numChannels;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            // Note: byteRate and blockAlign are not automatically calculated here.
            // They are typically set by methods like CalculateSizes in derived classes or before calling ToByteArray.
        }

        /// <summary>
        /// Converts the <see cref="WavePcmFormatHeader"/> object into its byte array representation (44 bytes).
        /// This method is virtual and can be overridden by derived classes to include audio data.
        /// Important: Ensure fields like <c>chunkSize</c>, <c>byteRate</c>, <c>blockAlign</c>, and <c>subchunk2Size</c> are correctly populated before calling.
        /// </summary>
        /// <returns>A byte array representing the WAV file header.</returns>
        public virtual byte[] ToByteArray()
        {
            // Ensure derived classes or callers correctly set byteRate and blockAlign before serialization if they are not default.
            // For the header alone, subchunk2Size might be 0 if there's no data part yet.
            // chunkSize would be 36 in that case.
            // This base implementation serializes the current state of the header fields.

            int headerSize = Marshal.SizeOf(this); // Should be 44 for this structure
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            byte[] rawData = new byte[headerSize];
            try
            {
                Marshal.StructureToPtr(this, headerPtr, false);
                Marshal.Copy(headerPtr, rawData, 0, headerSize);
            }
            finally
            {
                Marshal.FreeHGlobal(headerPtr);
            }
            return rawData;
        }

        /// <summary>
        /// Creates a <see cref="WavePcmFormatHeader"/> object from a byte array containing WAV file header data.
        /// </summary>
        /// <param name="headerBytes">A byte array representing the WAV file header (must be at least 44 bytes).</param>
        /// <returns>A new <see cref="WavePcmFormatHeader"/> object populated with information from the header bytes.</returns>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="headerBytes"/> is null or less than 44 bytes.</exception>
        /// <exception cref="System.Exception">Thrown if the header does not conform to expected WAV format identifiers (e.g., "RIFF", "WAVE", "fmt ", "data").</exception>
        public static WavePcmFormatHeader FromByteArray(byte[] headerBytes)
        {
            if (headerBytes == null || headerBytes.Length < 44)
            {
                throw new ArgumentException("Header byte array must be at least 44 bytes long.", nameof(headerBytes));
            }

            WavePcmFormatHeader waveHeader;

            GCHandle handle = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            try
            {
                waveHeader = (WavePcmFormatHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(WavePcmFormatHeader));
            }
            finally
            {
                handle.Free();
            }

            // Basic validation of WAV file structure
            if (waveHeader.chunkID[0] != 'R' || waveHeader.chunkID[1] != 'I' || waveHeader.chunkID[2] != 'F' || waveHeader.chunkID[3] != 'F')
                throw new System.Exception("Invalid WAV file: 'RIFF' chunk ID not found.");
            if (waveHeader.format[0] != 'W' || waveHeader.format[1] != 'A' || waveHeader.format[2] != 'V' || waveHeader.format[3] != 'E')
                throw new System.Exception("Only 'WAVE' format is supported.");
            if (waveHeader.subchunk1ID[0] != 'f' || waveHeader.subchunk1ID[1] != 'm' || waveHeader.subchunk1ID[2] != 't' || waveHeader.subchunk1ID[3] != ' ')
                throw new System.Exception("The first sub-chunk ('fmt ') not found or invalid.");
            if (waveHeader.subchunk2ID[0] != 'd' || waveHeader.subchunk2ID[1] != 'a' || waveHeader.subchunk2ID[2] != 't' || waveHeader.subchunk2ID[3] != 'a')
                // This check might be too strict if other chunks (like 'fact') can precede 'data'.
                // For a simple PCM WAV, 'data' is expected here.
                System.Diagnostics.Debug.WriteLine("Warning: Second sub-chunk ID is not 'data'. File may contain additional chunks or not be a simple PCM WAV.");


            return waveHeader;
        }
    }
}
