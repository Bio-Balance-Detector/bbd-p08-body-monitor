using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.Json;

namespace BBD.BodyMonitor
{
    [Obsolete]
    [Serializable]
    public class FftData
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

        public string Filename { get; internal set; }
        public string[] Tags { get; internal set; }

        public FftData() { }

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
