using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// [Obsolete("Use FftDataV3 instead.")] Represents Fast Fourier Transform (FFT) data, version 1. This is an older, obsolete version.
    /// </summary>
    [Obsolete]
    [Serializable]
    public class FftDataV1
    {
        /// <summary>
        /// Gets or sets the capture time. [Obsolete]
        /// </summary>
        public DateTime CaptureTime { get; set; }
        /// <summary>
        /// Gets or sets the duration of the FFT data in seconds. [Obsolete]
        /// </summary>
        public double Duration { get; set; }
        /// <summary>
        /// Gets or sets the number of samples the FFT is based on. [Obsolete]
        /// </summary>
        public int BasedOnSamplesCount { get; set; }
        /// <summary>
        /// Gets or sets the first frequency of the FFT. [Obsolete]
        /// </summary>
        public float FirstFrequency { get; set; }
        /// <summary>
        /// Gets or sets the last frequency of the FFT. [Obsolete]
        /// </summary>
        public float LastFrequency { get; set; }
        /// <summary>
        /// Gets or sets the frequency step of the FFT. [Obsolete]
        /// </summary>
        public float FrequencyStep { get; set; }
        /// <summary>
        /// Gets or sets the size of the FFT. [Obsolete]
        /// </summary>
        public int FftSize { get; set; }
        private float[] _magnitudeData { get; set; }
        /// <summary>
        /// Gets or sets the FFT magnitude data. [Obsolete]
        /// </summary>
        public float[] MagnitudeData { get; set; }
        /// <summary>
        /// Gets or sets the filename of the FFT data. [Obsolete]
        /// </summary>
        public string Filename { get; internal set; }
        /// <summary>
        /// Gets or sets the tags associated with the FFT data. [Obsolete]
        /// </summary>
        public string[] Tags { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the FftDataV1 class. [Obsolete]
        /// </summary>
        public FftDataV1() { }

        /// <summary>
        /// Saves the FftDataV1 object to a binary file. [Obsolete]
        /// </summary>
        /// <param name="FftDataV1">The FftDataV1 object to save.</param>
        /// <param name="pathToFile">The base path for the output file.</param>
        /// <param name="compress">True to compress the output into a .zip file.</param>
        public static void SaveAsBinary(FftDataV1 FftDataV1, string pathToFile, bool compress)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV1.Filename = filename;

            MemoryStream writeStream = new();

            BinaryFormatter formatter = new();
            formatter.Serialize(writeStream, FftDataV1);
            byte[] FftDataV1Binary = writeStream.ToArray();

            if (compress)
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Create);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
                ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{filename}.bfft");
                using BinaryWriter writer = new(fftFileEntry.Open());
                writer.Write(FftDataV1Binary, 0, FftDataV1Binary.Length);
            }
            else
            {
                File.WriteAllBytes($"{pathToFile}.bfft", FftDataV1Binary);
            }
        }

        /// <summary>
        /// Loads FftDataV1 from a file. [Obsolete]
        /// </summary>
        /// <param name="pathToFile">The path to the file.</param>
        /// <returns>An FftDataV1 object.</returns>
        public static FftDataV1 LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV1? FftDataV1 = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using FileStream readStream = new($"{pathToFile}.bfft", FileMode.Open);
                BinaryFormatter formatter = new();
                FftDataV1 = (FftDataV1)formatter.Deserialize(readStream);
            }

            if (File.Exists($"{pathToFile}.fft"))
            {
                FftDataV1 = JsonSerializer.Deserialize<FftDataV1>(File.ReadAllText($"{pathToFile}.fft"));
            }

            if (FftDataV1 == null && File.Exists($"{pathToFile}.zip"))
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Open);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Read);
                ZipArchiveEntry fftFileEntry = archive.GetEntry($"{filename}.fft");
                using StreamReader reader = new(fftFileEntry.Open());
                FftDataV1 = JsonSerializer.Deserialize<FftDataV1>(reader.ReadToEnd());
            }

            return FftDataV1 == null ? throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.") : FftDataV1;
        }
    }
}
