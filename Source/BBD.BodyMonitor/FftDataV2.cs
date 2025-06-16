using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// [Obsolete("Use FftDataV3 instead.")] Represents Fast Fourier Transform (FFT) data, version 2. This is an older, obsolete version with lazy loading capabilities.
    /// </summary>
    [Obsolete]
    [Serializable]
    public class FftDataV2
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
        /// Contains the FFT magnitude data without the DC component. Data is lazy-loaded. [Obsolete]
        /// </summary>
        public float[] MagnitudeData
        {
            get
            {
                if ((_magnitudeData == null) && (!string.IsNullOrWhiteSpace(Filename)))
                {
                    Load();
                }

                return _magnitudeData;
            }
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
        /// Gets or sets the filename of the FFT data. Used for lazy loading. [Obsolete]
        /// </summary>
        public string Filename { get; set; }
        /// <summary>
        /// Gets or sets the name of the FFT data. [Obsolete]
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the tags associated with the FFT data. [Obsolete]
        /// </summary>
        public string[] Tags { get; set; }
        /// <summary>
        /// Gets or sets the size of the source file. [Obsolete]
        /// </summary>
        public long FileSize { get; set; }
        /// <summary>
        /// Gets or sets the last modification time of the source file. [Obsolete]
        /// </summary>
        public DateTime FileModificationTime { get; set; }
        /// <summary>
        /// Gets the name of the Machine Learning Profile applied to this data. [Obsolete]
        /// </summary>
        public string MLProfileName { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FftDataV2 class. [Obsolete]
        /// </summary>
        public FftDataV2() { }

        /// <summary>
        /// Initializes a new instance of the FftDataV2 class from an FftData (V1) object. [Obsolete]
        /// </summary>
        /// <param name="fftDataV1">The FftData object to convert.</param>
        public FftDataV2(FftData fftDataV1)
        {
            CaptureTime = fftDataV1.CaptureTime;
            Duration = fftDataV1.Duration;
            BasedOnSamplesCount = fftDataV1.BasedOnSamplesCount;
            FirstFrequency = fftDataV1.FirstFrequency;
            LastFrequency = fftDataV1.LastFrequency;
            FrequencyStep = fftDataV1.FrequencyStep;
            FftSize = fftDataV1.FftSize;
            MagnitudeData = fftDataV1.MagnitudeData;
            Filename = fftDataV1.Filename;
            Tags = fftDataV1.Tags;
        }

        /// <summary>
        /// Downsamples the FFT data to a new frequency step. [Obsolete]
        /// </summary>
        /// <param name="frequencyStep">The target frequency step.</param>
        /// <returns>A new FftDataV2 instance with the downsampled data.</returns>
        public FftDataV2 Downsample(float frequencyStep)
        {
            if (FirstFrequency > 0)
            {
                throw new Exception("The FirstFrequency must be 0 if you want to use the MagnitudesPerHz property.");
            }

            if (FrequencyStep > frequencyStep)
            {
                throw new Exception("The FrequencyStep of the FFT dataset must be smaller then the frequency step that we resample to.");
            }

            FftDataV2 result = new()
            {
                CaptureTime = CaptureTime,
                Duration = Duration,
                BasedOnSamplesCount = BasedOnSamplesCount,
                FirstFrequency = FirstFrequency,
                FrequencyStep = frequencyStep,
                Name = Name,
                Tags = Tags
            };

            List<float> recalculatedValues = new()
            {
                0
            };

            for (int i = 0; i < _magnitudeData.Length; i++)
            {
                //var sourceBin = this.GetBinFromIndex(i);
                float sourceBinStartFrequency = i * FrequencyStep;
                float sourceBinEndFrequency = sourceBinStartFrequency + FrequencyStep;
                float sourceBinWidth = FrequencyStep;
                //var targetBin = result.GetBinFromIndex(recalculatedValues.Count - 1);
                float targetBinEndFrequency = recalculatedValues.Count * frequencyStep;

                if (sourceBinEndFrequency <= targetBinEndFrequency)
                {
                    recalculatedValues[^1] += _magnitudeData[i];
                }
                else
                {
                    // we need to do an interpolation
                    float ratio = (targetBinEndFrequency - sourceBinStartFrequency) / sourceBinWidth;

                    recalculatedValues[^1] += _magnitudeData[i] * ratio;
                    recalculatedValues.Add(_magnitudeData[i] * (1.0f - ratio));
                }
            }

            result.LastFrequency = frequencyStep * recalculatedValues.Count;
            result.FftSize = recalculatedValues.Count;
            result.MagnitudeData = recalculatedValues.Select(md => md * (FrequencyStep / frequencyStep)).ToArray();

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
            // calculate the median value of the magnitude data
            float median = MagnitudeData.OrderBy(x => x).Skip(MagnitudeData.Length / 2).First();

            MagnitudeStats result = new()
            {
                Min = minValue,
                MinIndex = Array.FindIndex(MagnitudeData, d => d == minValue),
                Max = maxValue,
                MaxIndex = Array.FindIndex(MagnitudeData, d => d == maxValue),
                Average = MagnitudeData.Average(),
                Median = median,
            };

            return result;
        }

        /// <summary>
        /// Saves the FftDataV2 object to a JSON file. [Obsolete]
        /// </summary>
        /// <param name="fftData">The FftDataV2 object to save.</param>
        /// <param name="pathToFile">The base path for the output file.</param>
        /// <param name="compress">True to compress the output into a .zip file.</param>
        public static void SaveAs(FftDataV2 fftData, string pathToFile, bool compress)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            fftData.Name = filename;
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
        /// Saves the FftDataV2 object to a binary file. [Obsolete]
        /// </summary>
        /// <param name="fftData">The FftDataV2 object to save.</param>
        /// <param name="pathToFile">The base path for the output file.</param>
        /// <param name="compress">True to compress the output into a .zip file.</param>
        /// <param name="mlProfileName">Optional ML profile name to incorporate into the filename.</param>
        public static void SaveAsBinary(FftDataV2 fftData, string pathToFile, bool compress, string mlProfileName = "")
        {
            pathToFile = GetMLProfiledNameWithoutExtension(pathToFile, mlProfileName);
            fftData.Name = Path.GetFileName(pathToFile);

            MemoryStream writeStream = new();

            BinaryFormatter formatter = new();
            formatter.Serialize(writeStream, fftData);
            byte[] fftDataBinary = writeStream.ToArray();

            if (compress)
            {
                using (FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Create))
                {
                    using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Create);
                    ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{fftData.Name}.bfft");
                    using BinaryWriter writer = new(fftFileEntry.Open());
                    writer.Write(fftDataBinary, 0, fftDataBinary.Length);
                }
                File.SetLastWriteTimeUtc($"{pathToFile}.zip", fftData.CaptureTime);
            }
            else
            {
                File.WriteAllBytes($"{pathToFile}.bfft", fftDataBinary);
                File.SetLastWriteTimeUtc($"{pathToFile}.bfft", fftData.CaptureTime);
            }
        }

        /// <summary>
        /// Loads FftDataV2 from a file. Handles automatic upgrade from FftData (V1). [Obsolete]
        /// </summary>
        /// <param name="pathToFile">The path to the file.</param>
        /// <returns>An FftDataV2 object.</returns>
        public static FftDataV2 LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV2? fftData = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using FileStream readStream = new($"{pathToFile}.bfft", FileMode.Open);
                BinaryFormatter formatter = new();
                try
                {
                    fftData = (FftDataV2)formatter.Deserialize(readStream);
                }
                catch
                {
                    _ = readStream.Seek(0, SeekOrigin.Begin);
                    FftData fftDataV1 = (FftData)formatter.Deserialize(readStream);
                    fftData = new FftDataV2(fftDataV1);
                    FftDataV2.SaveAsBinary(fftData, $"{pathToFile}_v2", false);
                }
            }

            if (File.Exists($"{pathToFile}.fft"))
            {
                fftData = JsonSerializer.Deserialize<FftDataV2>(File.ReadAllText($"{pathToFile}.fft"));
            }

            if ((fftData == null) && File.Exists($"{pathToFile}.zip"))
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Open);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Read);
                ZipArchiveEntry fftFileEntry = archive.GetEntry($"{filename}.fft");
                using StreamReader reader = new(fftFileEntry.Open());
                fftData = JsonSerializer.Deserialize<FftDataV2>(reader.ReadToEnd());
            }

            return fftData == null ? throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.") : fftData;
        }

        /// <summary>
        /// Generates a filename incorporating an ML profile name. [Obsolete]
        /// </summary>
        /// <param name="originalFilename">The original filename.</param>
        /// <param name="mlProfileName">The name of the ML profile.</param>
        /// <returns>The modified filename string.</returns>
        public static string GetMLProfiledNameWithoutExtension(string originalFilename, string mlProfileName)
        {
            string mlProfiledFilename = originalFilename;

            mlProfiledFilename = mlProfiledFilename[..^Path.GetExtension(mlProfiledFilename).Length];
            string filename = Path.GetFileNameWithoutExtension(mlProfiledFilename);
            if (!string.IsNullOrEmpty(mlProfileName))
            {
                filename = filename.Split("__")[0] + "__" + mlProfileName.Split("_")[0];
            }
            mlProfiledFilename = mlProfiledFilename[..^Path.GetFileName(mlProfiledFilename).Length] + filename;

            return mlProfiledFilename;
        }

        /// <summary>
        /// Loads magnitude data from the 'Filename'. [Obsolete]
        /// </summary>
        /// <param name="desiredMLProfileName">Optional ML profile name to try for filename variant.</param>
        public void Load(string desiredMLProfileName = "")
        {
            if (_magnitudeData == null)
            {
                string profiledFilename = GetMLProfiledNameWithoutExtension(Filename, desiredMLProfileName) + ".bfft";
                FftDataV2 data;
                if (File.Exists(profiledFilename))
                {
                    data = FftDataV2.LoadFrom(profiledFilename);
                    MLProfileName = desiredMLProfileName;
                }
                else
                {
                    data = FftDataV2.LoadFrom(Filename);
                }

                MagnitudeData = data.MagnitudeData;
                BasedOnSamplesCount = data.BasedOnSamplesCount;
                CaptureTime = data.CaptureTime;
                FrequencyStep = data.FrequencyStep;
                Duration = data.Duration;
                FftSize = data.FftSize;
                FirstFrequency = data.FirstFrequency;
                LastFrequency = data.LastFrequency;
            }
        }

        /// <summary>
        /// Applies a machine learning profile to the FFT data. [Obsolete]
        /// </summary>
        /// <param name="mlProfile">The MLProfile to apply.</param>
        /// <returns>A new FftDataV2 instance with the profile applied.</returns>
        public FftDataV2 ApplyMLProfile(MLProfile mlProfile)
        {
            FftDataV2 result = Downsample(mlProfile.FrequencyStep);

            if (FirstFrequency > mlProfile.MinFrequency)
            {
                throw new Exception("The dataset has a higher FirstFrequency than the ML profile's MinFrequency. This dataset can not be used with this profile.");
            }

            if (LastFrequency < mlProfile.MaxFrequency)
            {
                throw new Exception($"The dataset has a lower LastFrequency ({LastFrequency}) than the MaxFrequency ({mlProfile.MaxFrequency}) of ML profile '{mlProfile.Name}'. This dataset can not be used with this profile.");
            }

            int magnitudeDataIndexStart = 0;
            int magnitudeDataIndexEnd = 0;
            for (int ci = 0; ci < result.FftSize; ci++)
            {
                FftBin bin = result.GetBinFromIndex(ci);

                if (bin.EndFrequency <= mlProfile.MinFrequency)
                {
                    magnitudeDataIndexStart = ci + 1;
                }

                if (bin.StartFrequency < mlProfile.MaxFrequency)
                {
                    magnitudeDataIndexEnd = ci;
                }
            }
            magnitudeDataIndexEnd++;

            result = new FftDataV2()
            {
                CaptureTime = CaptureTime,
                Duration = Duration,
                BasedOnSamplesCount = BasedOnSamplesCount,
                FirstFrequency = result.GetBinFromIndex(magnitudeDataIndexStart).StartFrequency,
                LastFrequency = result.GetBinFromIndex(magnitudeDataIndexEnd - 1).EndFrequency,
                FrequencyStep = result.FrequencyStep,
                MLProfileName = mlProfile.Name,
                MagnitudeData = result.MagnitudeData[magnitudeDataIndexStart..magnitudeDataIndexEnd],
                Filename = Filename,
                Name = Name,
                Tags = Tags,
                FileSize = FileSize,
                FileModificationTime = FileModificationTime,
            };

            return result;
        }

        /// <summary>
        /// Clears the internally stored magnitude data. [Obsolete]
        /// </summary>
        public void Clear()
        {
            _magnitudeData = new float[0];
        }

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.FullTypeName = "BBD.BodyMonitor.FftData_v1";
        //    info.AddValue("CaptureTime", this.CaptureTime);
        //}

        /// <summary>
        /// Gets a string representation of the FFT frequency range. [Obsolete]
        /// </summary>
        /// <returns>A string like 'startHz-endHz'.</returns>
        public string GetFFTRange()
        {
            string startFrequency = (1 * FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string endFrequency = ((MagnitudeData.Length - 1) * FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string fftRange = $"{startFrequency}Hz-{endFrequency}Hz".Replace(".", "p");

            return fftRange;

        }

        /// <summary>
        /// Returns a string representation of the FftDataV2 object. [Obsolete]
        /// </summary>
        /// <returns>A string in the format 'FFT Data 'Name' FftSize x FrequencyStep Hz'.</returns>
        public override string ToString()
        {
            return $"FFT Data '{Name}' {FftSize}x{FrequencyStep:0.00} Hz";
        }

        /// <summary>
        /// Normalizes the MagnitudeData by dividing each value by the median. [Obsolete]
        /// </summary>
        public void ApplyMedianFilter()
        {
            MagnitudeStats stats = GetMagnitudeStats();

            for (int i = 0; i < MagnitudeData.Length; i++)
            {
                MagnitudeData[i] = MagnitudeData[i] / stats.Median;
            }
        }

        /// <summary>
        /// Applies a compressor effect to MagnitudeData. [Obsolete]
        /// </summary>
        /// <param name="power">The power to raise magnitudes to. Values < 1 compress, > 1 expand.</param>
        public void ApplyCompressorFilter(double power = 0.5)
        {
            if (power <= 0)
            {
                throw new Exception("Power must be greater than 0.");
            }

            for (int i = 0; i < MagnitudeData.Length; i++)
            {
                MagnitudeData[i] = (float)Math.Pow(MagnitudeData[i], power);
            }
        }
    }
}
