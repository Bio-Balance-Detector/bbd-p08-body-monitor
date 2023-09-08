using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Filters;
using NWaves.Signals;
using NWaves.Transforms;

namespace BBD.BodyMonitor.Models
{
    public enum FftFillMethod { NoFill, ZeroFill, DataFill }
    public class FftDataBlockCache
    {
        private readonly Dictionary<long, FftDataV3> cache = new();
        private readonly RealFft fft;
        public int Samplerate { get; }
        public int FftSize { get; }
        public float BlockLength { get; }
        public float ResampleFFTResolutionToHz { get; }

        public FftDataBlockCache(int samplerate, int fftSize, float blockLength, float resampleFFTResolutionToHz)
        {
            Samplerate = samplerate;
            FftSize = fftSize;
            BlockLength = blockLength;
            ResampleFFTResolutionToHz = resampleFFTResolutionToHz;
            fft = new RealFft(fftSize);
        }

        public FftDataV3? Get(DataBlock dataBlock, FftFillMethod fftFillMethod = FftFillMethod.NoFill)
        {
            long dataBlockHash = dataBlock.GetHash();

            lock (cache)
            {
                if (cache.ContainsKey(dataBlockHash))
                {
                    return cache[dataBlockHash];
                }
                else
                {
                    // generate an NWaves signal
                    float[] samplesToAdd = dataBlock.Data;
                    int sampleCount = samplesToAdd.Length;
                    if (sampleCount < FftSize)
                    {
                        //_logger.LogWarning($"There are not enough samples in the buffer to do the FFT calculation with {_config.Postprocessing.FFTSize:N0} bins. Try to increase the samplerate ({_config.Acquisition.Samplerate} Hz) and/or the postprocessing datablock size ({_config.Postprocessing.DataBlock} seconds) by the combined factor of {((float)_config.Postprocessing.FFTSize / sampleCount):0.00}.");

                        // we need to fill up the buffer to the FFT size otherwise the FFT calculation fails
                        List<float> samplesToAddList = dataBlock.Data.ToList();

                        switch (fftFillMethod)
                        {
                            case FftFillMethod.ZeroFill:
                                // Method A - fill with zeros
                                samplesToAddList.AddRange(Enumerable.Repeat(0.0f, FftSize - sampleCount));
                                break;
                            case FftFillMethod.DataFill:
                                // Method B - fill with last sample
                                int dataToAdd = FftSize - sampleCount;
                                while (dataToAdd > 0)
                                {
                                    samplesToAddList.AddRange(samplesToAdd.Take(Math.Min(samplesToAdd.Length, dataToAdd)));
                                    dataToAdd -= samplesToAdd.Length;
                                }
                                break;
                            case FftFillMethod.NoFill:
                                // Method C - cancel the postprocessing
                                return null;
                        }

                        samplesToAdd = samplesToAddList.ToArray();

                    }
                    DiscreteSignal signal = new(Samplerate, samplesToAdd, true);

                    //signal.Amplify(short.MaxValue / 1.0f);

                    //Random rnd = new Random();
                    //FileStream waveFileStream = new FileStream($"o:\\Work\\BBD.BodyMonitor\\2022-10-10\\AD2_20221010_{rnd.Next(999999):000000}__{this.Samplerate}sps_ch1.wav", FileMode.Create);
                    //WaveFile waveFile = new WaveFile(signal, 16);
                    //waveFile.SaveTo(waveFileStream, false);
                    //waveFileStream.Close();


                    FftDataV3 resampledFFTData = CreateFftData(signal, dataBlock.StartTime, sampleCount);

                    cache.Add(dataBlockHash, resampledFFTData);

                    return resampledFFTData;
                }
            }

        }

        /// <summary>
        /// Create FFT data from a signal and apply some filters
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="startTime"></param>
        /// <param name="sampleCount"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public FftDataV3 CreateFftData(DiscreteSignal signal, DateTimeOffset startTime, int sampleCount)
        {
            FftDataV3 fftData = new()
            {
                Start = startTime,
                End = startTime.AddSeconds(BlockLength),
                BasedOnSamplesCount = sampleCount,
                FirstFrequency = 0,
                LastFrequency = Samplerate / 2,
                FftSize = FftSize,
            };

            //logger.LogInformation($"#{bi.ToString("0000")} signal.Samples.Length: {sampleCount:N0} | FFT size: {config.Postprocessing.FFTSize:N0}");

            if (fft.Size > sampleCount)
            {
                throw new Exception($"The FFT size ({fft.Size:N0}) is larger than the number of samples ({sampleCount:N0}) in the signal.");
            }

            // calculate magnitude spectrum with normalization and ignore the 0th coefficient (DC component)
            fftData.MagnitudeData = fft.MagnitudeSpectrum(signal, normalize: true).Samples[1..];

            fftData.FrequencyStep = fftData.LastFrequency / (fftData.MagnitudeData.Length - 1);

            //MagnitudeStats magnitudeStats = fftData.GetMagnitudeStats();

            //maxValues.Add(magnitudeStats.Max);
            //int maxIndexStart = Math.Max(0, magnitudeStats.MaxIndex - 2);
            //int maxIndexEnd = Math.Min(fftData.MagnitudeData.Length - 1, magnitudeStats.MaxIndex + 2 + 1);

            //_logger.LogTrace($"#{threadId} The maximum magnitude values are ( {string.Join(" | ", fftData.MagnitudeData[maxIndexStart..maxIndexEnd].Select(m => string.Format("{0,10:N}", m * 1000 * 1000)))} ) µV around {fftData.GetBinFromIndex(magnitudeStats.MaxIndex)}.");

            //MagnitudeStats magnitudeStatsBeforeDownsample = fftData.GetMagnitudeStats();

            FftDataV3 resampledFFTData = fftData.Downsample(ResampleFFTResolutionToHz);

            //MagnitudeStats magnitudeStatsAfterDownsample = resampledFFTData.GetMagnitudeStats();

            resampledFFTData = resampledFFTData.RemoveNoiseFromTheMains();

            //MagnitudeStats magnitudeStatsAfterNoiseRemoval = resampledFFTData.GetMagnitudeStats();

            resampledFFTData = resampledFFTData.MakeItRelative();

            //MagnitudeStats magnitudeStatsAfterRelative1 = resampledFFTData.GetMagnitudeStats();

            //resampledFFTData = resampledFFTData.MakeItRelative();

            //MagnitudeStats magnitudeStatsAfterRelative2 = resampledFFTData.GetMagnitudeStats();

            //Console.WriteLine($"{magnitudeStatsBeforeDownsample} {magnitudeStatsAfterDownsample} {magnitudeStatsAfterNoiseRemoval} {magnitudeStatsAfterRelative1} {magnitudeStatsAfterRelative2}");

            return resampledFFTData;
        }
    }
}
