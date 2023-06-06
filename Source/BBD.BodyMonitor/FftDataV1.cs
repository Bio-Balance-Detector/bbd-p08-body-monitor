using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace BBD.BodyMonitor
{
    [Obsolete]
    [Serializable]
    public class FftDataV1
    {
        public DateTime CaptureTime { get; set; }
        public double Duration { get; set; }
        public int BasedOnSamplesCount { get; set; }
        public float FirstFrequency { get; set; }
        public float LastFrequency { get; set; }
        public float FrequencyStep { get; set; }
        public int FftSize { get; set; }
        private float[] _magnitudeData { get; set; }
        public float[] MagnitudeData { get; set; }
        public string Filename { get; internal set; }
        public string[] Tags { get; internal set; }

        public FftDataV1() { }

        public static void SaveAsBinary(FftDataV1 FftDataV1, string pathToFile, bool compress)
        {
            pathToFile = pathToFile.Substring(0, pathToFile.Length - Path.GetExtension(pathToFile).Length);
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV1.Filename = filename;

            var writeStream = new MemoryStream();

            var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            formatter.Serialize(writeStream, FftDataV1);
#pragma warning restore SYSLIB0011
            byte[] FftDataV1Binary = writeStream.ToArray();

            if (compress)
            {
                using (FileStream zipToOpen = new FileStream($"{pathToFile}.zip", FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{filename}.bfft");
                        using (BinaryWriter writer = new BinaryWriter(fftFileEntry.Open()))
                        {
                            writer.Write(FftDataV1Binary, 0, FftDataV1Binary.Length);
                        }
                    }
                }
            }
            else
            {
                File.WriteAllBytes($"{pathToFile}.bfft", FftDataV1Binary);
            }
        }

        public static FftDataV1 LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile.Substring(0, pathToFile.Length - Path.GetExtension(pathToFile).Length);
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV1 FftDataV1 = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using (var readStream = new FileStream($"{pathToFile}.bfft", FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
                    FftDataV1 = (FftDataV1)formatter.Deserialize(readStream);
#pragma warning restore SYSLIB0011
                }
            }

            if (File.Exists($"{pathToFile}.fft"))
            {
                FftDataV1 = JsonSerializer.Deserialize<FftDataV1>(File.ReadAllText($"{pathToFile}.fft"));
            }

            if (FftDataV1 == null && File.Exists($"{pathToFile}.zip"))
            {
                using (FileStream zipToOpen = new FileStream($"{pathToFile}.zip", FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry fftFileEntry = archive.GetEntry($"{filename}.fft");
                        using (StreamReader reader = new StreamReader(fftFileEntry.Open()))
                        {
                            FftDataV1 = JsonSerializer.Deserialize<FftDataV1>(reader.ReadToEnd());
                        }
                    }
                }
            }

            if (FftDataV1 == null)
            {
                throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.");
            }

            return FftDataV1;
        }
    }
}
