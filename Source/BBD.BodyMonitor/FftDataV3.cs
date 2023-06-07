using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BBD.BodyMonitor
{
    [Serializable]
    public class FftDataV3
    {
        public string Version { get; } = "3.0";
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public double? Duration => Start.HasValue && End.HasValue ? (End.Value - Start.Value).TotalSeconds : null;
        public int BasedOnSamplesCount { get; set; }
        public float FirstFrequency { get; set; }
        public float LastFrequency { get; set; }
        public float FrequencyStep { get; set; }
        public int FftSize { get; set; }
        private float[]? _magnitudeData { get; set; } = null;
        private List<string> appliedFilters = new();
        /// <summary>
        /// Contains the FFT magnitude data without the DC component
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
        [JsonIgnore]
        public string Filename { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string[] Tags { get; set; } = new string[0];
        [JsonIgnore]
        public long FileSize { get; set; }
        [JsonIgnore]
        public DateTime? FileModificationTimeUtc { get; set; }
        public string MLProfileName { get; private set; } = string.Empty;
        public string[] AppliedFilters => appliedFilters.ToArray();

        public FftDataV3() { }

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

        public void ClearData()
        {
            _magnitudeData = new float[0];
        }

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.FullTypeName = "BBD.BodyMonitor.FftData_v1";
        //    info.AddValue("CaptureTime", this.CaptureTime);
        //}

        public string GetFFTRange()
        {
            string startFrequency = (1 * FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string endFrequency = ((MagnitudeData.Length - 1) * FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string fftRange = $"{startFrequency}Hz-{endFrequency}Hz".Replace(".", "p");

            return fftRange;

        }

        public override string ToString()
        {
            return $"FFT Data '{Name}' {FftSize}x{FrequencyStep:0.00} Hz";
        }

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
        /// Compress or expand the values of the FFT data by calculating the given power of their values.
        /// </summary>
        /// <param name="power">The power to raise the FFT values to. Use <1.0 numbers for compression and >1.0 numbers for expansion.</param>
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
