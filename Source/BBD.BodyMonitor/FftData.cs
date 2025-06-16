using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// [Obsolete("Use FftDataV3 instead.")] Represents Fast Fourier Transform (FFT) data. This is an older, obsolete version.
    /// </summary>
    [Obsolete]
    [Serializable]
    public class FftData
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
        /// Contains the FFT magnitude data without the DC component. [Obsolete]
        /// </summary>
        public float[] MagnitudeData
        {
            get => _magnitudeData;
            set
            {
                if (_magnitudeData != null)
                {
                    throw new Exception("MagnitudeData can be set only once and it was already yet before.");
                }

                _magnitudeData = value;
            }
        }

        /// <summary>
        /// Gets or sets the filename of the FFT data. [Obsolete]
        /// </summary>
        public string Filename { get; internal set; }
        /// <summary>
        /// Gets or sets the tags associated with the FFT data. [Obsolete]
        /// </summary>
        public string[] Tags { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the FftData class. [Obsolete]
        /// </summary>
        public FftData() { }

        /// <summary>
        /// Downsamples the FFT data to a new frequency step. [Obsolete]
        /// </summary>
        /// <param name="frequencyStep">The target frequency step.</param>
        /// <returns>A new FftData instance with the downsampled data.</returns>
        public FftData Downsample(float frequencyStep)
        {
            if (FirstFrequency > 0)
            {
                throw new Exception("The FirstFrequency must be 0 if you want to use the MagnitudesPerHz property.");
            }

            if (FrequencyStep > frequencyStep)
            {
                throw new Exception("The FrequencyStep of the FFT dataset must be smaller then the frequency step that we resample to.");
            }

            FftData result = new()
            {
                CaptureTime = CaptureTime,
                Duration = Duration,
                BasedOnSamplesCount = BasedOnSamplesCount,
                FirstFrequency = FirstFrequency,
                FrequencyStep = frequencyStep,
                Filename = Filename,
                Tags = Tags
            };

            List<float> recalculatedValues = new()
            {
                0
            };

            for (int i = 0; i < _magnitudeData.Length; i++)
            {
                FftBin sourceBin = GetBinFromIndex(i);
                FftBin targetBin = result.GetBinFromIndex(recalculatedValues.Count - 1);

                if (sourceBin.EndFrequency <= targetBin.EndFrequency)
                {
                    recalculatedValues[^1] += _magnitudeData[i];
                }
                else
                {
                    // we need to do an interpolation
                    float ratio = (targetBin.EndFrequency - sourceBin.StartFrequency) / sourceBin.Width;

                    recalculatedValues[^1] += _magnitudeData[i] * ratio;
                    recalculatedValues.Add(_magnitudeData[i] * (1.0f - ratio));
                }
            }

            result.LastFrequency = frequencyStep * recalculatedValues.Count;
            result.FftSize = recalculatedValues.Count;
            result.MagnitudeData = recalculatedValues.ToArray();

            return result;
        }

        /// <summary>
        /// Gets an FftBin object describing the bin at the specified index. [Obsolete]
        /// </summary>
        /// <param name="index">The index of the bin.</param>
        /// <param name="frequencyStep">Optional. The frequency step to use for calculation.</param>
        /// <returns>An FftBin object.</returns>
        public FftBin GetBinFromIndex(int index, float? frequencyStep = null)
        {
            frequencyStep ??= FrequencyStep;

            return new FftBin()
            {
                Index = index,
                StartFrequency = index * frequencyStep.Value,
                EndFrequency = (index + 1) * frequencyStep.Value,
                MiddleFrequency = (index + 0.5f) * frequencyStep.Value,
                Width = FrequencyStep
            };
        }

        /// <summary>
        /// Calculates and returns statistics for the MagnitudeData. [Obsolete]
        /// </summary>
        /// <returns>A MagnitudeStats object.</returns>
        public MagnitudeStats GetMagnitudeStats()
        {
            float minValue = MagnitudeData.Min();
            float maxValue = MagnitudeData.Max();

            MagnitudeStats result = new()
            {
                Min = minValue,
                MinIndex = Array.FindIndex(MagnitudeData, d => d == minValue),
                Max = maxValue,
                MaxIndex = Array.FindIndex(MagnitudeData, d => d == maxValue),
                Average = MagnitudeData.Average()
            };

            return result;
        }

        /// <summary>
        /// Saves the FftData object to a JSON file. [Obsolete]
        /// </summary>
        /// <param name="fftData">The FftData object to save.</param>
        /// <param name="pathToFile">The base path for the output file.</param>
        /// <param name="compress">True to compress the output into a .zip file.</param>
        public static void SaveAs(FftData fftData, string pathToFile, bool compress)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            fftData.Filename = filename;
            string fftDataJson = JsonSerializer.Serialize(fftData, new JsonSerializerOptions() { WriteIndented = true });

            if (compress)
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Create);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
                ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{filename}.fft");
                using StreamWriter writer = new(fftFileEntry.Open());
                writer.Write(fftDataJson);
            }
            else
            {
                _ = File.WriteAllTextAsync($"{pathToFile}.fft", fftDataJson);
            }
        }

        /// <summary>
        /// Saves the FftData object to a binary file. [Obsolete]
        /// </summary>
        /// <param name="fftData">The FftData object to save.</param>
        /// <param name="pathToFile">The base path for the output file.</param>
        /// <param name="compress">True to compress the output into a .zip file.</param>
        public static void SaveAsBinary(FftData fftData, string pathToFile, bool compress)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            fftData.Filename = filename;

            MemoryStream writeStream = new();

            BinaryFormatter formatter = new();
            formatter.Serialize(writeStream, fftData);
            byte[] fftDataBinary = writeStream.ToArray();

            if (compress)
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Create);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
                ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{filename}.bfft");
                using BinaryWriter writer = new(fftFileEntry.Open());
                writer.Write(fftDataBinary, 0, fftDataBinary.Length);
            }
            else
            {
                File.WriteAllBytes($"{pathToFile}.bfft", fftDataBinary);
            }
        }

        /// <summary>
        /// Loads FftData from a file. [Obsolete]
        /// </summary>
        /// <param name="pathToFile">The path to the file.</param>
        /// <returns>An FftData object.</returns>
        public static FftData LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftData? fftData = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using FileStream readStream = new($"{pathToFile}.bfft", FileMode.Open);
                BinaryFormatter formatter = new();
                fftData = (FftData)formatter.Deserialize(readStream);
            }

            if (File.Exists($"{pathToFile}.fft"))
            {
                fftData = JsonSerializer.Deserialize<FftData>(File.ReadAllText($"{pathToFile}.fft"));
            }

            if (fftData == null && File.Exists($"{pathToFile}.zip"))
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Open);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Read);
                ZipArchiveEntry fftFileEntry = archive.GetEntry($"{filename}.fft");
                using StreamReader reader = new(fftFileEntry.Open());
                fftData = JsonSerializer.Deserialize<FftData>(reader.ReadToEnd());
            }

            return fftData == null ? throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.") : fftData;
        }

        /// <summary>
        /// Applies a machine learning profile to the FFT data. [Obsolete]
        /// </summary>
        /// <param name="frequencyStep">The target frequency step.</param>
        /// <param name="minFrequency">The minimum frequency for the profile.</param>
        /// <param name="maxFrequency">The maximum frequency for the profile.</param>
        /// <returns>A new FftData instance with the profile applied.</returns>
        public FftData ApplyMLProfile(float frequencyStep, float minFrequency, float maxFrequency)
        {
            FftData result = Downsample(frequencyStep);

            int magnitudeDataIndexStart = 0;
            int magnitudeDataIndexEnd = 0;
            for (int ci = 0; ci < result.FftSize; ci++)
            {
                FftBin bin = result.GetBinFromIndex(ci);

                if (bin.EndFrequency <= minFrequency)
                {
                    magnitudeDataIndexStart = ci + 1;
                }

                if (bin.StartFrequency < maxFrequency)
                {
                    magnitudeDataIndexEnd = ci;
                }
            }
            magnitudeDataIndexEnd++;

            result = new FftData()
            {
                CaptureTime = CaptureTime,
                Duration = Duration,
                BasedOnSamplesCount = BasedOnSamplesCount,
                FirstFrequency = result.GetBinFromIndex(magnitudeDataIndexStart).StartFrequency,
                LastFrequency = result.GetBinFromIndex(magnitudeDataIndexEnd - 1).EndFrequency,
                FrequencyStep = result.FrequencyStep,
                Filename = Filename,
                Tags = Tags,
                MagnitudeData = result.MagnitudeData[magnitudeDataIndexStart..magnitudeDataIndexEnd]
            };

            return result;
        }

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.FullTypeName = "BBD.BodyMonitor.FftData_v1";
        //    info.AddValue("CaptureTime", this.CaptureTime);
        //}
    }
}
