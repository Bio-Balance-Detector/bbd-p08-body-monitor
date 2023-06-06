using BBD.BodyMonitor.Sessions;
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
        public double? Duration
        {
            get
            {
                if (this.Start.HasValue && this.End.HasValue)
                {
                    return (this.End.Value - this.Start.Value).TotalSeconds;
                }
                else
                {
                    return null;
                }
            }
        }
        public int BasedOnSamplesCount { get; set; }
        public float FirstFrequency { get; set; }
        public float LastFrequency { get; set; }
        public float FrequencyStep { get; set; }
        public int FftSize { get; set; }
        private float[]? _magnitudeData { get; set; } = null;
        private List<string> appliedFilters = new List<string>();
        /// <summary>
        /// Contains the FFT magnitude data without the DC component
        /// </summary>
        public float[] MagnitudeData
        {
            get
            {
                if ((_magnitudeData == null) && (!String.IsNullOrWhiteSpace(this.Filename)))
                {
                    this.Load();
                }

                return _magnitudeData == null ? new float[0] : _magnitudeData;
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
        public string Filename { get; set; } = String.Empty;
        public string Name { get; set; } = String.Empty;
        public string[] Tags { get; set; } = new string[0];
        [JsonIgnore]
        public long FileSize { get; set; }
        [JsonIgnore]
        public DateTime? FileModificationTimeUtc { get; set; }
        public string MLProfileName { get; private set; } = String.Empty;
        public string[] AppliedFilters
        {
            get
            {
                return appliedFilters.ToArray();
            }
        }

        public FftDataV3() { }

        public FftDataV3(FftDataV2 fftDataV2)
        {
            DateTimeOffset startTime = new DateTimeOffset(fftDataV2.CaptureTime.Ticks, new TimeSpan(+2,0,0));

            this.Start = startTime;
            this.End = startTime.AddSeconds(fftDataV2.Duration);
            this.BasedOnSamplesCount = fftDataV2.BasedOnSamplesCount;
            this.FirstFrequency = fftDataV2.FirstFrequency;
            this.LastFrequency = fftDataV2.LastFrequency;
            this.FrequencyStep = fftDataV2.FrequencyStep;
            this.FftSize = fftDataV2.FftSize;
            this._magnitudeData = fftDataV2.MagnitudeData;
            this.Filename = fftDataV2.Filename;
            this.Tags = fftDataV2.Tags;
            this.MLProfileName = fftDataV2.MLProfileName;
        }

        public FftDataV3(FftDataV3 fftData)
        {
            this.Start = fftData.Start;
            this.End = fftData.End;
            this.BasedOnSamplesCount = fftData.BasedOnSamplesCount;
            this.FirstFrequency = fftData.FirstFrequency;
            this.LastFrequency = fftData.LastFrequency;
            this.FrequencyStep = fftData.FrequencyStep;
            this.FftSize = fftData.FftSize;
            this.Filename = fftData.Filename;
            this.Name = fftData.Name;
            this.Tags = fftData.Tags;
            this.FileSize = fftData.FileSize;
            this.FileModificationTimeUtc = fftData.FileModificationTimeUtc;
            this.MLProfileName = fftData.MLProfileName;
            this.appliedFilters = new List<string>(fftData.AppliedFilters);
        }

        public FftDataV3 Downsample(float frequencyStep)
        {
            if (this.FirstFrequency > 0)
            {
                throw new Exception("The FirstFrequency must be 0 if you want to use the MagnitudesPerHz property.");
            }

            if (this.FrequencyStep > frequencyStep)
            {
                throw new Exception("The FrequencyStep of the FFT dataset must be smaller then the frequency step that we resample to.");
            }

            FftDataV3 result = new FftDataV3(this);
            result.FrequencyStep = frequencyStep;

            var recalculatedValues = new List<float>();
            recalculatedValues.Add(0);

            for (int i = 0; i < _magnitudeData.Length; i++)
            {
                //var sourceBin = this.GetBinFromIndex(i);
                var sourceBinStartFrequency = i * this.FrequencyStep;
                var sourceBinEndFrequency = sourceBinStartFrequency + this.FrequencyStep;
                var sourceBinWidth = this.FrequencyStep;
                //var targetBin = result.GetBinFromIndex(recalculatedValues.Count - 1);
                var targetBinEndFrequency = (recalculatedValues.Count) * frequencyStep;

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
            result.MagnitudeData = recalculatedValues.Select(md => md * (this.FrequencyStep / frequencyStep)).ToArray();
            result.appliedFilters.Add($"BBD.Downsample({frequencyStep})");

            return result;
        }

        public FftBin GetBinFromIndex(int index, float? frequencyStep = null)
        {
            if (frequencyStep == null)
            {
                frequencyStep = this.FrequencyStep;
            }

            return new FftBin()
            {
                Index = index,
                StartFrequency = index * frequencyStep.Value,
                EndFrequency = (index + 1) * frequencyStep.Value,
                MiddleFrequency = (index + 0.5f) * frequencyStep.Value,
                Width = this.FrequencyStep
            };
        }

        public MagnitudeStats GetMagnitudeStats()
        {
            float minValue = this.MagnitudeData.Min();
            float maxValue = this.MagnitudeData.Max();
            // calculate the median value of the magnitude data
            float median = this.MagnitudeData.OrderBy(x => x).Skip(this.MagnitudeData.Length / 2).First();

            MagnitudeStats result = new MagnitudeStats()
            {
                Min = minValue,
                MinIndex = Array.FindIndex(this.MagnitudeData, d => d == minValue),
                Max = maxValue,
                MaxIndex = Array.FindIndex(this.MagnitudeData, d => d == maxValue),
                Average = this.MagnitudeData.Average(),
                Median = median,
            };

            return result;
        }

        public static void SaveAsJson(FftDataV3 fftData, string pathToFile, bool compress)
        {
            pathToFile = pathToFile.Substring(0, pathToFile.Length - Path.GetExtension(pathToFile).Length);
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            fftData.Name = filename;
            string fftDataJson = JsonSerializer.Serialize(fftData, new JsonSerializerOptions() { WriteIndented = true });

            if (compress)
            {
                using (FileStream zipToOpen = new FileStream($"{pathToFile}.zip", FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{filename}.fft");
                        using (StreamWriter writer = new StreamWriter(fftFileEntry.Open()))
                        {
                            writer.Write(fftDataJson);
                        }
                    }
                }
            }
            else
            {
                File.WriteAllTextAsync($"{pathToFile}.fft", fftDataJson);
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

            var writeStream = new MemoryStream();

            var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
            formatter.Serialize(writeStream, fftData);
#pragma warning restore SYSLIB0011
            byte[] fftDataBinary = writeStream.ToArray();

            if (compress)
            {
                using (FileStream zipToOpen = new FileStream($"{pathToFile}.zip", FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                    {
                        ZipArchiveEntry fftFileEntry = archive.CreateEntry($"{fftData.Name}.bfft");
                        using (BinaryWriter writer = new BinaryWriter(fftFileEntry.Open()))
                        {
                            writer.Write(fftDataBinary, 0, fftDataBinary.Length);
                        }
                    }
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

        public static FftDataV3 LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile.Substring(0, pathToFile.Length - Path.GetExtension(pathToFile).Length);
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV3? fftData = null;
            Exception? ex = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using (var readStream = new FileStream($"{pathToFile}.bfft", FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
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
                            readStream.Seek(0, SeekOrigin.Begin);
                            var fftDataV2 = (FftDataV2)formatter.Deserialize(readStream);
                            fftData = new FftDataV3(fftDataV2);

                            var fi = new FileInfo(pathToFile);
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
                                readStream.Seek(0, SeekOrigin.Begin);
                                var fftDataV1 = (FftData)formatter.Deserialize(readStream);
                                FftDataV2 fftDataV2 = new FftDataV2(fftDataV1);
                                fftData = new FftDataV3(fftDataV2);

                                var fi = new FileInfo(pathToFile);
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

            if ((fftData == null) && (File.Exists($"{pathToFile}.zip")))
            {
                using (FileStream zipToOpen = new FileStream($"{pathToFile}.zip", FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry? fftFileEntry = archive.GetEntry($"{filename}.fft");

                        if (fftFileEntry != null)
                        {
                            using (StreamReader reader = new StreamReader(fftFileEntry.Open()))
                            {
                                fftData = JsonSerializer.Deserialize<FftDataV3>(reader.ReadToEnd());
                            }
                        }
                    }
                }
            }

            if (fftData == null)
            {
                throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.*.", ex);
            }

            return fftData;
        }

        public static string GetMLProfiledFilenameWithoutExtension(string originalFilename, string mlProfileName)
        {
            string mlProfiledFilename = originalFilename;

            mlProfiledFilename = mlProfiledFilename.Substring(0, mlProfiledFilename.Length - Path.GetExtension(mlProfiledFilename).Length);
            string filename = Path.GetFileNameWithoutExtension(mlProfiledFilename);
            if (!String.IsNullOrEmpty(mlProfileName))
            {
                filename = filename.Split("__")[0] + "__" + mlProfileName.Split("_")[0];
            }
            mlProfiledFilename = mlProfiledFilename.Substring(0, mlProfiledFilename.Length - Path.GetFileName(mlProfiledFilename).Length) + filename;

            return mlProfiledFilename;
        }

        public void Load(string desiredMLProfileName = "")
        {
            if (this._magnitudeData == null)
            {
                string profiledFilename = GetMLProfiledFilenameWithoutExtension(this.Filename, desiredMLProfileName) + ".bfft";
                FftDataV3 data;
                if (File.Exists(profiledFilename))
                {
                    data = FftDataV3.LoadFrom(profiledFilename);
                    this.MLProfileName = desiredMLProfileName;
                }
                else
                {
                    data = FftDataV3.LoadFrom(this.Filename);
                    this.MLProfileName = data.MLProfileName;
                }

                this.Start = data.Start;
                this.End = data.End;
                this.BasedOnSamplesCount = data.BasedOnSamplesCount;
                this.FirstFrequency = data.FirstFrequency;
                this.LastFrequency = data.LastFrequency;
                this.FrequencyStep = data.FrequencyStep;
                this.FftSize = data.FftSize;
                this.MagnitudeData = data.MagnitudeData;
                this.appliedFilters = new List<string>(data.AppliedFilters);
            }
        }

        public FftDataV3 ApplyMLProfile(MLProfile mlProfile)
        {
            FftDataV3 result = this.Downsample(mlProfile.FrequencyStep);

            if (this.FirstFrequency > mlProfile.MinFrequency)
            {
                throw new Exception("The dataset has a higher FirstFrequency than the ML profile's MinFrequency. This dataset can not be used with this profile.");
            }

            if (this.LastFrequency < mlProfile.MaxFrequency)
            {
                throw new Exception($"The dataset has a lower LastFrequency ({this.LastFrequency}) than the MaxFrequency ({mlProfile.MaxFrequency}) of ML profile '{mlProfile.Name}'. This dataset can not be used with this profile.");
            }

            int magnitudeDataIndexStart = 0;
            int magnitudeDataIndexEnd = 0;
            for (int ci = 0; ci < result.FftSize; ci++)
            {
                var bin = result.GetBinFromIndex(ci);

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
            string startFrequency = (1 * this.FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string endFrequency = ((this.MagnitudeData.Length - 1) * this.FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            string fftRange = $"{startFrequency}Hz-{endFrequency}Hz".Replace(".", "p");

            return fftRange;

        }

        public override string ToString()
        {
            return $"FFT Data '{Name}' {this.FftSize}x{this.FrequencyStep:0.00} Hz";
        }

        public void ApplyMedianFilter()
        {
            var stats = this.GetMagnitudeStats();

            for (int i = 0; i < this.MagnitudeData.Length; i++)
            {
                this.MagnitudeData[i] = this.MagnitudeData[i] / stats.Median;
            }

            this.appliedFilters.Add($"BBD.Median()");
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

            for (int i = 0; i < this.MagnitudeData.Length; i++)
            {
                this.MagnitudeData[i] = (float)Math.Pow(this.MagnitudeData[i], power);
            }

            this.appliedFilters.Add($"BBD.Compressor({power})");
        }
    }
}
