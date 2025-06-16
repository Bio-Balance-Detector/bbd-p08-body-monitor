using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor
{
    /// <summary>
    /// Represents Fast Fourier Transform (FFT) data, version 3. Includes metadata about the capture, frequency characteristics, and the magnitude data itself. Provides methods for data manipulation, loading, and saving.
    /// </summary>
    [Serializable]
    public class FftDataV3
    {
        /// <summary>
        /// Gets the version of this FftData structure.
        /// </summary>
        public string Version { get; } = "3.0";
        /// <summary>
        /// Gets or sets the start date and time of the data capture.
        /// </summary>
        public DateTimeOffset? Start { get; set; }
        /// <summary>
        /// Gets or sets the end date and time of the data capture.
        /// </summary>
        public DateTimeOffset? End { get; set; }
        /// <summary>
        /// Gets the duration of the data capture in seconds. Calculated from Start and End times.
        /// </summary>
        public double? Duration => Start.HasValue && End.HasValue ? (End.Value - Start.Value).TotalSeconds : null;
        /// <summary>
        /// Gets or sets the number of original samples this FFT data is based on.
        /// </summary>
        public int BasedOnSamplesCount { get; set; }
        /// <summary>
        /// Gets or sets the first frequency (e.g., DC offset or starting frequency) in Hz.
        /// </summary>
        public float FirstFrequency { get; set; }
        /// <summary>
        /// Gets or sets the last frequency represented in the FFT data in Hz.
        /// </summary>
        public float LastFrequency { get; set; }
        /// <summary>
        /// Gets or sets the frequency step or resolution in Hz between FFT bins.
        /// </summary>
        public float FrequencyStep { get; set; }
        /// <summary>
        /// Gets or sets the size of the FFT (number of bins).
        /// </summary>
        public int FftSize { get; set; }
        /// <summary>
        /// Internal storage for the FFT magnitude data. Null if not loaded.
        /// </summary>
        private float[]? _magnitudeData { get; set; } = null;
        /// <summary>
        /// Internal list of filters applied to this instance.
        /// </summary>
        private List<string> appliedFilters = new();
        /// <summary>
        /// Gets or sets the array of FFT magnitude values. The data is lazy-loaded from 'Filename' if not already in memory. The setter throws if data has already been set.
        /// </summary>
        public float[] MagnitudeData
        {
            get
            {
                if ((_magnitudeData == null) && (!string.IsNullOrWhiteSpace(Filename)))
                {
                    Load();
                }

                return _magnitudeData ?? (new float[0]);
            }
            set
            {
                if (_magnitudeData != null)
                {
                    throw new Exception("MagnitudeData can be set only once and it was already set before.");
                }

                _magnitudeData = value;
            }
        }
        /// <summary>
        /// Gets or sets the original filename from which this FFT data was loaded. Used for lazy loading. Marked with [JsonIgnore].
        /// </summary>
        [JsonIgnore]
        public string Filename { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets a descriptive name for this FFT data instance, often derived from the filename.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets an array of tags associated with this FFT data for categorization or filtering.
        /// </summary>
        public string[] Tags { get; set; } = new string[0];
        /// <summary>
        /// Gets or sets the size of the source file in bytes. Marked with [JsonIgnore].
        /// </summary>
        [JsonIgnore]
        public long FileSize { get; set; }
        /// <summary>
        /// Gets or sets the last modification UTC timestamp of the source file. Marked with [JsonIgnore].
        /// </summary>
        [JsonIgnore]
        public DateTime? FileModificationTimeUtc { get; set; }
        /// <summary>
        /// Gets the name of the Machine Learning Profile that was applied to this FFT data, if any.
        /// </summary>
        public string MLProfileName { get; private set; } = string.Empty;
        /// <summary>
        /// Gets an array of strings describing the filters that have been applied to this FFT data instance.
        /// </summary>
        public string[] AppliedFilters => appliedFilters.ToArray();

        /// <summary>
        /// Initializes a new instance of the FftDataV3 class.
        /// </summary>
        public FftDataV3() { }

        /// <summary>
        /// Initializes a new instance of the FftDataV3 class from an obsolete FftDataV2 object. This constructor is obsolete.
        /// </summary>
        [Obsolete]
        public FftDataV3(FftDataV2 fftDataV2)
        {
            DateTimeOffset startTime = new(fftDataV2.CaptureTime.Ticks, new TimeSpan(+2, 0, 0));

            Start = startTime;
            End = startTime.AddSeconds(fftDataV2.Duration);
            BasedOnSamplesCount = fftDataV2.BasedOnSamplesCount;
            FirstFrequency = fftDataV2.FirstFrequency;
            LastFrequency = fftDataV2.LastFrequency;
            FrequencyStep = fftDataV2.FrequencyStep;
            FftSize = fftDataV2.FftSize;
            _magnitudeData = fftDataV2.MagnitudeData;
            Filename = fftDataV2.Filename;
            Tags = fftDataV2.Tags;
            MLProfileName = fftDataV2.MLProfileName;
        }

        /// <summary>
        /// Initializes a new instance of the FftDataV3 class as a copy of another FftDataV3 instance.
        /// </summary>
        public FftDataV3(FftDataV3 fftData)
        {
            Start = fftData.Start;
            End = fftData.End;
            BasedOnSamplesCount = fftData.BasedOnSamplesCount;
            FirstFrequency = fftData.FirstFrequency;
            LastFrequency = fftData.LastFrequency;
            FrequencyStep = fftData.FrequencyStep;
            FftSize = fftData.FftSize;
            Filename = fftData.Filename;
            Name = fftData.Name;
            Tags = fftData.Tags;
            FileSize = fftData.FileSize;
            FileModificationTimeUtc = fftData.FileModificationTimeUtc;
            MLProfileName = fftData.MLProfileName;
            appliedFilters = new List<string>(fftData.AppliedFilters);
        }

        /// <summary>
        /// Downsamples the FFT data to a new frequency step. The first frequency must be 0.
        /// </summary>
        /// <param name="frequencyStep">The target frequency step in Hz. Must be larger than the current FrequencyStep.</param>
        /// <returns>A new FftDataV3 instance with the downsampled data.</returns>
        /// <exception cref="System.Exception">Thrown if FirstFrequency is not 0 or if the target frequencyStep is smaller than the current.</exception>
        public FftDataV3 Downsample(float frequencyStep)
        {
            if (FirstFrequency > 0)
            {
                throw new Exception("The FirstFrequency must be 0 if you want to use the MagnitudesPerHz property.");
            }

            if (FrequencyStep > frequencyStep)
            {
                throw new Exception("The FrequencyStep of the FFT dataset must be smaller then the frequency step that we resample to.");
            }

            FftDataV3 result = new(this)
            {
                FrequencyStep = frequencyStep
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
            result.appliedFilters.Add($"BBD.Downsample({frequencyStep})");

            return result;
        }

        /// <summary>
        /// Gets an FftBin object describing the bin at the specified index.
        /// </summary>
        /// <param name="index">The index of the bin.</param>
        /// <param name="frequencyStep">Optional. The frequency step to use for calculation. Defaults to the instance's FrequencyStep.</param>
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
        /// Calculates and returns statistics (min, max, average, median) for the MagnitudeData.
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
        /// Saves the FftDataV3 object to a JSON file, optionally compressing it into a ZIP archive.
        /// </summary>
        /// <param name="fftData">The FftDataV3 object to save.</param>
        /// <param name="pathToFile">The base path for the output file (extension will be .fft or .zip).</param>
        /// <param name="compress">True to compress the output into a .zip file; false for an uncompressed .fft file.</param>
        public static void SaveAsJson(FftDataV3 fftData, string pathToFile, bool compress)
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
        /// Saves the FftDataV3 object to a binary file using BinaryFormatter, optionally compressing it and applying an ML profile name to the filename.
        /// </summary>
        /// <param name="fftData">The FftDataV3 object to save.</param>
        /// <param name="pathToFile">The base path for the output file.</param>
        /// <param name="compress">True to compress the output into a .zip file; false for an uncompressed .bfft file.</param>
        /// <param name="mlProfileName">Optional ML profile name to incorporate into the filename.</param>
        public static void SaveAsBinary(FftDataV3 fftData, string pathToFile, bool compress, string mlProfileName = "")
        {
            if (fftData == null)
            {
                throw new ArgumentNullException(nameof(fftData));
            }

            pathToFile = GetMLProfiledFilenameWithoutExtension(pathToFile, mlProfileName);
            fftData.Name = Path.GetFileName(pathToFile);

            MemoryStream writeStream = new();

            BinaryFormatter formatter = new();
#pragma warning disable SYSLIB0011
            formatter.Serialize(writeStream, fftData);
#pragma warning restore SYSLIB0011
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

                if (fftData.Start.HasValue)
                {
                    File.SetLastWriteTimeUtc($"{pathToFile}.zip", fftData.Start.Value.UtcDateTime);
                }
            }
            else
            {
                File.WriteAllBytes($"{pathToFile}.bfft", fftDataBinary);
                if (fftData.Start.HasValue)
                {
                    File.SetLastWriteTimeUtc($"{pathToFile}.bfft", fftData.Start.Value.UtcDateTime);
                }
            }
        }

        /// <summary>
        /// Loads FftDataV3 from a file. It attempts to load .bfft (binary) first, then .fft (JSON), then .zip (compressed JSON). Handles automatic upgrade from older FftDataV1/V2 binary formats. This method is obsolete; consider specific load methods if format is known.
        /// </summary>
        /// <param name="pathToFile">The path to the file (extension will be auto-detected).</param>
        /// <returns>An FftDataV3 object.</returns>
        /// <exception cref="System.Exception">Thrown if the file cannot be opened or parsed.</exception>
        [Obsolete]
        public static FftDataV3 LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile[..^Path.GetExtension(pathToFile).Length];
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV3? fftData = null;
            Exception? ex = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using (FileStream readStream = new($"{pathToFile}.bfft", FileMode.Open))
                {
                    BinaryFormatter formatter = new();
#pragma warning disable SYSLIB0011
                    try
                    {
                        fftData = (FftDataV3)formatter.Deserialize(readStream);
                    }
                    catch (Exception ev3)
                    {
                        ex = ev3;

                        try
                        {
                            _ = readStream.Seek(0, SeekOrigin.Begin);
                            FftDataV2 fftDataV2 = (FftDataV2)formatter.Deserialize(readStream);
                            fftData = new FftDataV3(fftDataV2);

                            FileInfo fi = new(pathToFile);
                            fftData.Filename = pathToFile;
                            fftData.FileSize = fi.Length;
                            fftData.FileModificationTimeUtc = fi.LastWriteTimeUtc;
                            fftData.Name = Path.GetFileNameWithoutExtension($"{pathToFile}");

                            FftDataV3.SaveAsBinary(fftData, $"{pathToFile}_v3", false);
                        }
                        catch (Exception ev2)
                        {
                            ex = ev2;
                            try
                            {
                                _ = readStream.Seek(0, SeekOrigin.Begin);
                                FftData fftDataV1 = (FftData)formatter.Deserialize(readStream);
                                FftDataV2 fftDataV2 = new(fftDataV1);
                                fftData = new FftDataV3(fftDataV2);

                                FileInfo fi = new(pathToFile);
                                fftData.Filename = pathToFile;
                                fftData.FileSize = fi.Length;
                                fftData.FileModificationTimeUtc = fi.LastWriteTimeUtc;
                                fftData.Name = Path.GetFileNameWithoutExtension($"{pathToFile}");

                                FftDataV3.SaveAsBinary(fftData, $"{pathToFile}_v3", false);
                            }
                            catch (Exception ev1)
                            {
                                ex = ev1;
                            }
                        }
                    }
#pragma warning restore SYSLIB0011
                }

                if (File.Exists($"{pathToFile}_v3.bfft"))
                {
                    Debug.WriteLine($"Upgrading binary data file '{pathToFile}.bfft' to V3.");
                    File.Delete($"{pathToFile}.bfft");
                    File.Move($"{pathToFile}_v3.bfft", $"{pathToFile}.bfft");
                }
            }

            if (File.Exists($"{pathToFile}.fft"))
            {
                fftData = JsonSerializer.Deserialize<FftDataV3>(File.ReadAllText($"{pathToFile}.fft"));
            }

            if ((fftData == null) && File.Exists($"{pathToFile}.zip"))
            {
                using FileStream zipToOpen = new($"{pathToFile}.zip", FileMode.Open);
                using ZipArchive archive = new(zipToOpen, ZipArchiveMode.Read);
                ZipArchiveEntry? fftFileEntry = archive.GetEntry($"{filename}.fft");

                if (fftFileEntry != null)
                {
                    using StreamReader reader = new(fftFileEntry.Open());
                    fftData = JsonSerializer.Deserialize<FftDataV3>(reader.ReadToEnd());
                }
            }

            return fftData == null ? throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.*.", ex) : fftData;
        }

        /// <summary>
        /// Generates a filename without extension, incorporating an ML profile name.
        /// </summary>
        /// <param name="originalFilename">The original filename.</param>
        /// <param name="mlProfileName">The name of the ML profile.</param>
        /// <returns>The modified filename string.</returns>
        public static string GetMLProfiledFilenameWithoutExtension(string originalFilename, string mlProfileName)
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
        /// Loads magnitude data from the 'Filename', optionally trying a filename variant with the desired ML profile name. This method is obsolete due to its complex behavior and reliance on global state (Filename property).
        /// </summary>
        /// <param name="desiredMLProfileName">Optional ML profile name to try for filename variant.</param>
        [Obsolete]
        public void Load(string desiredMLProfileName = "")
        {
            if (_magnitudeData == null)
            {
                string profiledFilename = GetMLProfiledFilenameWithoutExtension(Filename, desiredMLProfileName) + ".bfft";
                FftDataV3 data;
                if (File.Exists(profiledFilename))
                {
                    data = FftDataV3.LoadFrom(profiledFilename);
                    MLProfileName = desiredMLProfileName;
                }
                else
                {
                    data = FftDataV3.LoadFrom(Filename);
                    MLProfileName = data.MLProfileName;
                }

                Start = data.Start;
                End = data.End;
                BasedOnSamplesCount = data.BasedOnSamplesCount;
                FirstFrequency = data.FirstFrequency;
                LastFrequency = data.LastFrequency;
                FrequencyStep = data.FrequencyStep;
                FftSize = data.FftSize;
                MagnitudeData = data.MagnitudeData;
                appliedFilters = new List<string>(data.AppliedFilters);
            }
        }

        /// <summary>
        /// Applies a machine learning profile to the FFT data. This involves downsampling and frequency range selection.
        /// </summary>
        /// <param name="mlProfile">The MLProfile to apply.</param>
        /// <returns>A new FftDataV3 instance with the profile applied.</returns>
        /// <exception cref="System.Exception">Thrown if the data's frequency range is incompatible with the profile.</exception>
        public FftDataV3 ApplyMLProfile(MLProfile mlProfile)
        {
            FftDataV3 result = Downsample(mlProfile.FrequencyStep);

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

            result = new FftDataV3()
            {
                Start = result.Start,
                End = result.End,
                BasedOnSamplesCount = result.BasedOnSamplesCount,
                FirstFrequency = result.GetBinFromIndex(magnitudeDataIndexStart).StartFrequency,
                LastFrequency = result.GetBinFromIndex(magnitudeDataIndexEnd - 1).EndFrequency,
                FrequencyStep = result.FrequencyStep,
                FftSize = result.FftSize,
                MLProfileName = mlProfile.Name,
                MagnitudeData = result.MagnitudeData[magnitudeDataIndexStart..magnitudeDataIndexEnd],
                Filename = result.Filename,
                Name = result.Name,
                Tags = result.Tags,
                FileSize = result.FileSize,
                FileModificationTimeUtc = result.FileModificationTimeUtc,
                appliedFilters = new List<string>(result.appliedFilters)
            };

            return result;
        }

        /// <summary>
        /// Clears the internally stored magnitude data, effectively resetting it to an empty array.
        /// </summary>
        public void ClearData()
        {
            _magnitudeData = new float[0];
        }

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.FullTypeName = "BBD.BodyMonitor.FftData_v1";
        //    info.AddValue("CaptureTime", this.CaptureTime);
        //}

        /// <summary>
        /// Gets a string representation of the effective FFT frequency range (excluding DC).
        /// </summary>
        /// <returns>A string like 'startHz-endHz' with 'p' replacing '.' (e.g., '0p5Hz-100Hz').</returns>
        public string GetFFTRange()
        {
            string startFrequency = (1 * FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string endFrequency = ((MagnitudeData.Length - 1) * FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string fftRange = $"{startFrequency}Hz-{endFrequency}Hz".Replace(".", "p");

            return fftRange;

        }

        /// <summary>
        /// Returns a string representation of the FftDataV3 object.
        /// </summary>
        /// <returns>A string in the format 'FFT Data 'Name' FftSize x FrequencyStep Hz'.</returns>
        public override string ToString()
        {
            return $"FFT Data '{Name}' {FftSize}x{FrequencyStep:0.00} Hz";
        }

        /// <summary>
        /// Normalizes the MagnitudeData by dividing each value by the median of all magnitudes. Adds 'BBD.Median()' to AppliedFilters.
        /// </summary>
        public void ApplyMedianFilter()
        {
            MagnitudeStats stats = GetMagnitudeStats();

            for (int i = 0; i < MagnitudeData.Length; i++)
            {
                MagnitudeData[i] = MagnitudeData[i] / stats.Median;
            }

            appliedFilters.Add($"BBD.Median()");
        }

        /// <summary>
        /// Applies a compressor effect to MagnitudeData by raising each value to the specified power. Adds 'BBD.Compressor(power)' to AppliedFilters.
        /// </summary>
        /// <param name="power">The power to raise magnitudes to. Values < 1 compress, > 1 expand. Must be > 0.</param>
        /// <exception cref="System.Exception">Thrown if power is not greater than 0.</exception>
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

            appliedFilters.Add($"BBD.Compressor({power})");
        }
    }
}
