using System.Runtime.InteropServices;

namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Represents a complete WAV file in PCM format, including the header and audio data.
    /// This class inherits from <see cref="WavePcmFormatHeader"/> and adds the actual sound data.
    /// </summary>
    public class WavePcmFormat : WavePcmFormatHeader
    {
        /// <summary>
        /// Gets or sets the actual sound data of the WAV file as a byte array.
        /// Each sample is encoded according to the <see cref="WavePcmFormatHeader.BitsPerSample"/> and <see cref="WavePcmFormatHeader.NumChannels"/>.
        /// </summary>
        public byte[] Data { get; set; } = new byte[0];

        /// <summary>
        /// Initializes a new instance of the <see cref="WavePcmFormat"/> class.
        /// This constructor is often used for deserialization purposes.
        /// </summary>
        public WavePcmFormat() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WavePcmFormat"/> class with specified audio parameters and data.
        /// </summary>
        /// <param name="numChannels">The number of audio channels (e.g., 1 for mono, 2 for stereo). Default is 2.</param>
        /// <param name="sampleRate">The sample rate in Hertz (e.g., 44100). Default is 44100 Hz.</param>
        /// <param name="bitsPerSample">The number of bits per sample (e.g., 16). Default is 16.</param>
        /// <param name="data">The raw audio data as a byte array. If null, an empty byte array is used. Default is null.</param>
        public WavePcmFormat(ushort numChannels = 2, uint sampleRate = 44100, ushort bitsPerSample = 16, byte[]? data = null)
        {
            Data = data ?? new byte[0];
            NumChannels = numChannels;
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            // Other header fields like BlockAlign, ByteRate, ChunkSize, Subchunk2Size will be calculated by ToByteArray or need manual setting if created this way.
        }

        private void CalculateSizes()
        {
            // These calculations assume standard PCM format where BlockAlign and ByteRate can be derived.
            // BitsPerSample must be a multiple of 8 for this calculation of BlockAlign to be straightforward.
            blockAlign = (ushort)(NumChannels * (BitsPerSample / 8));
            byteRate = SampleRate * blockAlign;
            subchunk2Size = (uint)Data.Length;
            chunkSize = 36 + subchunk2Size; // 36 represents the size of the header before the data chunk (44 total - 8 for "RIFF" and chunksize itself)
        }

        /// <summary>
        /// Converts the <see cref="WavePcmFormat"/> object, including its header and data, into a byte array representing a complete WAV file.
        /// Header fields related to size (e.g., <see cref="WavePcmFormatHeader.chunkSize"/>, <see cref="WavePcmFormatHeader.subchunk2Size"/>) are recalculated based on the current data.
        /// </summary>
        /// <returns>A byte array containing the WAV file data.</returns>
        public override byte[] ToByteArray()
        {
            CalculateSizes(); // Ensure all size-related fields in the header are up-to-date

            // Create a temporary header structure for marshaling, as 'this' includes 'Data' field which is not part of the 44-byte header.
            WavePcmFormatHeader headerStruct = new WavePcmFormatHeader
            {
                chunkID = this.chunkID,
                chunkSize = this.chunkSize,
                format = this.format,
                subchunk1ID = this.subchunk1ID,
                subchunk1Size = this.subchunk1Size,
                audioFormat = this.audioFormat,
                NumChannels = this.NumChannels,
                SampleRate = this.SampleRate,
                byteRate = this.byteRate,
                blockAlign = this.blockAlign,
                BitsPerSample = this.BitsPerSample,
                subchunk2ID = this.subchunk2ID,
                subchunk2Size = this.subchunk2Size,
                // DataChuckPosition is not part of the marshaled header struct directly, it's an offset.
            };


            int headerSize = 44; // Standard WAV header size
            IntPtr headerPtr = Marshal.AllocHGlobal(headerSize);
            byte[] rawData = new byte[headerSize + Data.Length];

            try
            {
                Marshal.StructureToPtr(headerStruct, headerPtr, false);
                Marshal.Copy(headerPtr, rawData, 0, headerSize);
            }
            finally
            {
                Marshal.FreeHGlobal(headerPtr);
            }

            Array.Copy(Data, 0, rawData, headerSize, Data.Length); // Data starts after the 44-byte header
            return rawData;
        }

        /// <summary>
        /// Creates a <see cref="WavePcmFormat"/> object from a byte array containing WAV file header data.
        /// This method expects at least 44 bytes for the header. The actual audio data is not populated by this method from the input byte array; only header fields are parsed.
        /// </summary>
        /// <param name="headerBytes">A byte array representing the WAV file header (at least 44 bytes).</param>
        /// <returns>A new <see cref="WavePcmFormat"/> object populated with information from the header bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="headerBytes"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="headerBytes"/> is less than 44 bytes long.</exception>
        /// <exception cref="Exception">Thrown if the header does not conform to expected WAV format identifiers (e.g., "RIFF", "WAVE", "fmt ").</exception>
        public static new WavePcmFormat FromByteArray(byte[] headerBytes) // 'new' keyword hides the base class method if one exists with the same signature.
        {
            if (headerBytes == null)
            {
                throw new ArgumentNullException(nameof(headerBytes));
            }
            if (headerBytes.Length < 44)
            {
                throw new ArgumentException("Header byte array must be at least 44 bytes long.", nameof(headerBytes));
            }

            WavePcmFormat waveFile = new WavePcmFormat(); // Use the parameterless constructor

            GCHandle handle = GCHandle.Alloc(headerBytes, GCHandleType.Pinned);
            try
            {
                // Marshal the 44-byte header into the base WavePcmFormatHeader part of waveFile
                Marshal.PtrToStructure(handle.AddrOfPinnedObject(), (object)waveFile); // Cast to object to use the correct Marshal.PtrToStructure overload
            }
            finally
            {
                handle.Free();
            }

            // Validate standard WAV chunks
            if (waveFile.chunkID[0] != 'R' || waveFile.chunkID[1] != 'I' || waveFile.chunkID[2] != 'F' || waveFile.chunkID[3] != 'F')
            {
                throw new System.Exception("Invalid WAV file: 'RIFF' chunk ID not found.");
            }

            if (waveFile.format[0] != 'W' || waveFile.format[1] != 'A' || waveFile.format[2] != 'V' || waveFile.format[3] != 'E')
            {
                throw new System.Exception("Only 'WAVE' format is supported.");
            }

            if (waveFile.subchunk1ID[0] != 'f' || waveFile.subchunk1ID[1] != 'm' || waveFile.subchunk1ID[2] != 't' || waveFile.subchunk1ID[3] != ' ')
            {
                throw new System.Exception("The first sub-chunk in the WAV file must be 'fmt '.");
            }

            // The 'data' subchunk ID might not be immediately after 'fmt ' if there are other chunks (e.g. 'fact' chunk for non-PCM).
            // This simple parser assumes 'data' is next or seeks for it. For robustness, a real parser would search for the 'data' chunk.
            // For now, we assume the provided headerBytes *is* the start of the file and contains these in order.
            // If subchunk2ID is part of the first 44 bytes and not 'data', it's an issue for this simplified parser.
            if (waveFile.subchunk2ID[0] != 'd' || waveFile.subchunk2ID[1] != 'a' || waveFile.subchunk2ID[2] != 't' || waveFile.subchunk2ID[3] != 'a')
            {
                // This might be too strict if other chunks can appear before 'data'.
                // However, for a simple PCM WAV, 'data' is expected.
                // Consider logging a warning or being more flexible if other chunk types are common.
                System.Diagnostics.Debug.WriteLine("Warning: Second sub-chunk is not 'data'. File may contain extra chunks or not be a simple PCM WAV.");
            }

            // Note: The Data field is not populated from 'headerBytes' here.
            // This method is primarily for parsing the header structure.
            // The CalculateSizes method is called to ensure internal consistency if Data were populated separately.
            // waveFile.CalculateSizes(); // This would set subchunk2Size to 0 if Data is empty.
                                     // If headerBytes actually contains data size, subchunk2Size from Marshal would be correct.

            return waveFile;
        }
    }
}
