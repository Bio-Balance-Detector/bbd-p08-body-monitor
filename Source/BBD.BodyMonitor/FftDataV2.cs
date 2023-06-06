using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace BBD.BodyMonitor
{
    [Obsolete]
    [Serializable]
    public class FftDataV2
    {
        public DateTime CaptureTime { get; set; }
        public double Duration { get; set; }
        public int BasedOnSamplesCount { get; set; }
        public float FirstFrequency { get; set; }
        public float LastFrequency { get; set; }
        public float FrequencyStep { get; set; }
        public int FftSize { get; set; }
        private float[] _magnitudeData { get; set; }
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
        public string Filename { get; set; }
        public string Name { get; set; }
        public string[] Tags { get; set; }
        public long FileSize { get; set; }
        public DateTime FileModificationTime { get; set; }
        public string MLProfileName { get; private set; }

        public FftDataV2() { }

        public FftDataV2(FftData fftDataV1)
        {
            this.CaptureTime = fftDataV1.CaptureTime;
            this.Duration = fftDataV1.Duration;
            this.BasedOnSamplesCount = fftDataV1.BasedOnSamplesCount;
            this.FirstFrequency = fftDataV1.FirstFrequency;
            this.LastFrequency = fftDataV1.LastFrequency;
            this.FrequencyStep = fftDataV1.FrequencyStep;
            this.FftSize = fftDataV1.FftSize;
            this.MagnitudeData = fftDataV1.MagnitudeData;
            this.Filename = fftDataV1.Filename;
            this.Tags = fftDataV1.Tags;
        }

        public FftDataV2 Downsample(float frequencyStep)
        {
            if (this.FirstFrequency > 0)
            {
                throw new Exception("The FirstFrequency must be 0 if you want to use the MagnitudesPerHz property.");
            }

            if (this.FrequencyStep > frequencyStep)
            {
                throw new Exception("The FrequencyStep of the FFT dataset must be smaller then the frequency step that we resample to.");
            }

            FftDataV2 result = new FftDataV2()
            {
                CaptureTime = this.CaptureTime,
                Duration = this.Duration,
                BasedOnSamplesCount = this.BasedOnSamplesCount,
                FirstFrequency = this.FirstFrequency,
                FrequencyStep = frequencyStep,
                Name = this.Name,
                Tags = this.Tags
            };

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

        public static void SaveAs(FftDataV2 fftData, string pathToFile, bool compress)
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

        public static void SaveAsBinary(FftDataV2 fftData, string pathToFile, bool compress, string mlProfileName = "")
        {
            pathToFile = GetMLProfiledNameWithoutExtension(pathToFile, mlProfileName);
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
                File.SetLastWriteTimeUtc($"{pathToFile}.zip", fftData.CaptureTime);
            }
            else
            {
                File.WriteAllBytes($"{pathToFile}.bfft", fftDataBinary);
                File.SetLastWriteTimeUtc($"{pathToFile}.bfft", fftData.CaptureTime);
            }
        }

        public static FftDataV2 LoadFrom(string pathToFile)
        {
            pathToFile = pathToFile.Substring(0, pathToFile.Length - Path.GetExtension(pathToFile).Length);
            string filename = Path.GetFileNameWithoutExtension(pathToFile);
            FftDataV2 fftData = null;

            if (File.Exists($"{pathToFile}.bfft"))
            {
                using (var readStream = new FileStream($"{pathToFile}.bfft", FileMode.Open))
                {
                    var formatter = new BinaryFormatter();
#pragma warning disable SYSLIB0011
                    try
                    {
                        fftData = (FftDataV2)formatter.Deserialize(readStream);
                    }
                    catch
                    {
                        readStream.Seek(0, SeekOrigin.Begin);
                        var fftDataV1 = (FftData)formatter.Deserialize(readStream);
                        fftData = new FftDataV2(fftDataV1);
                        FftDataV2.SaveAsBinary(fftData, $"{pathToFile}_v2", false);
                    }
#pragma warning restore SYSLIB0011
                }
            }

            if (File.Exists($"{pathToFile}.fft"))
            {
                fftData = JsonSerializer.Deserialize<FftDataV2>(File.ReadAllText($"{pathToFile}.fft"));
            }

            if ((fftData == null) && (File.Exists($"{pathToFile}.zip")))
            {
                using (FileStream zipToOpen = new FileStream($"{pathToFile}.zip", FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry fftFileEntry = archive.GetEntry($"{filename}.fft");
                        using (StreamReader reader = new StreamReader(fftFileEntry.Open()))
                        {
                            fftData = JsonSerializer.Deserialize<FftDataV2>(reader.ReadToEnd());
                        }
                    }
                }
            }

            if (fftData == null)
            {
                throw new Exception($"Could not open the .dfft, .fft or .zip file for '{pathToFile}'.");
            }

            return fftData;
        }

        public static string GetMLProfiledNameWithoutExtension(string originalFilename, string mlProfileName)
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
                string profiledFilename = GetMLProfiledNameWithoutExtension(this.Filename, desiredMLProfileName) + ".bfft";
                FftDataV2 data;
                if (File.Exists(profiledFilename))
                {
                    data = FftDataV2.LoadFrom(profiledFilename);
                    this.MLProfileName = desiredMLProfileName;
                }
                else
                {
                    data = FftDataV2.LoadFrom(this.Filename);
                }

                this.MagnitudeData = data.MagnitudeData;
                this.BasedOnSamplesCount = data.BasedOnSamplesCount;
                this.CaptureTime = data.CaptureTime;
                this.FrequencyStep = data.FrequencyStep;
                this.Duration = data.Duration;
                this.FftSize = data.FftSize;
                this.FirstFrequency = data.FirstFrequency;
                this.LastFrequency = data.LastFrequency;
            }
        }

        public FftDataV2 ApplyMLProfile(MLProfile mlProfile)
        {
            FftDataV2 result = this.Downsample(mlProfile.FrequencyStep);

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

            result = new FftDataV2()
            {
                CaptureTime = this.CaptureTime,
                Duration = this.Duration,
                BasedOnSamplesCount = this.BasedOnSamplesCount,
                FirstFrequency = result.GetBinFromIndex(magnitudeDataIndexStart).StartFrequency,
                LastFrequency = result.GetBinFromIndex(magnitudeDataIndexEnd - 1).EndFrequency,
                FrequencyStep = result.FrequencyStep,
                MLProfileName = mlProfile.Name,
                MagnitudeData = result.MagnitudeData[magnitudeDataIndexStart..magnitudeDataIndexEnd],
                Filename = this.Filename,
                Name = this.Name,
                Tags = this.Tags,
                FileSize = this.FileSize,
                FileModificationTime = this.FileModificationTime,
            };

            return result;
        }

        public void Clear()
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
        }
    }
}
