using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Filters;
using NWaves.Signals;
using NWaves.Transforms;

namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Defines the method to use when the number of samples in a data block is less than the required FFT size.
    /// </summary>
    public enum FftFillMethod
    {
        /// <summary>
        /// Do not perform FFT if the sample count is insufficient. The Get method will return null.
        /// </summary>
        NoFill,
        /// <summary>
        /// Pad the data block with zeros until it reaches the FFT size.
        /// </summary>
        ZeroFill,
        /// <summary>
        /// Pad the data block by repeating existing samples until it reaches the FFT size.
        /// </summary>
        DataFill
    }

    /// <summary>
    /// Manages a cache of FFT (Fast Fourier Transform) data generated from <see cref="DataBlock"/> instances.
    /// This class helps to avoid redundant FFT calculations by storing and retrieving previously computed FFT results.
    /// </summary>
    public class FftDataBlockCache
    {
        private readonly Dictionary<long, FftDataV3> cache = new();
        private readonly RealFft fft;

        /// <summary>
        /// Gets the samplerate used for the FFT calculations, in Hz.
        /// </summary>
        public int Samplerate { get; }

        /// <summary>
        /// Gets the size of the FFT (number of points).
        /// </summary>
        public int FftSize { get; }

        /// <summary>
        /// Gets the length of the data blocks in seconds that this cache processes.
        /// </summary>
        public float BlockLength { get; }

        /// <summary>
        /// Gets the target resolution in Hz to which the FFT data will be resampled.
        /// </summary>
        public float ResampleFFTResolutionToHz { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FftDataBlockCache"/> class.
        /// </summary>
        /// <param name="samplerate">The samplerate of the input signals, in Hz.</param>
        /// <param name="fftSize">The size of the FFT to be performed (number of points).</param>
        /// <param name="blockLength">The length of the data blocks in seconds.</param>
        /// <param name="resampleFFTResolutionToHz">The target frequency resolution in Hz for resampling the FFT output. This determines the density of frequency bins in the final <see cref="FftDataV3"/> result after downsampling.</param>
        public FftDataBlockCache(int samplerate, int fftSize, float blockLength, float resampleFFTResolutionToHz)
        {
            Samplerate = samplerate;
            FftSize = fftSize;
            BlockLength = blockLength;
            ResampleFFTResolutionToHz = resampleFFTResolutionToHz;
            fft = new RealFft(fftSize);
        }

        /// <summary>
        /// Retrieves FFT data for the given <see cref="DataBlock"/>.
        /// If the FFT data for this block is already cached, it is returned from the cache.
        /// Otherwise, a new FFT is computed, cached, and then returned.
        /// </summary>
        /// <param name="dataBlock">The data block for which to get FFT data.</param>
        /// <param name="fftFillMethod">Specifies how to handle cases where the data block contains fewer samples than <see cref="FftSize"/>. Defaults to <see cref="FftFillMethod.NoFill"/>.</param>
        /// <returns>An <see cref="FftDataV3"/> object containing the FFT result, or null if <paramref name="fftFillMethod"/> is <see cref="FftFillMethod.NoFill"/> and the data block is smaller than <see cref="FftSize"/>.</returns>
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
                    // Ensure the signal has at least FftSize samples before creating DiscreteSignal for FFT
                    // This might involve padding if sampleCount was originally < FftSize and a fill method was used.
                    // If sampleCount >= FftSize, it takes the first FftSize samples.
                    DiscreteSignal signal = new(Samplerate, samplesToAdd.Take(FftSize), true);

                    //signal.Amplify(short.MaxValue / 1.0f);

                    //Random rnd = new Random();
                    //FileStream waveFileStream = new FileStream($"o:\\Work\\BBD.BodyMonitor\\2022-10-10\\AD2_20221010_{rnd.Next(999999):000000}__{this.Samplerate}sps_ch1.wav", FileMode.Create);
                    //WaveFile waveFile = new WaveFile(signal, 16);
                    //waveFile.SaveTo(waveFileStream, false);
                    //waveFileStream.Close();

                    // CreateFftData expects the signal to have FftSize samples.
                    FftDataV3 resampledFFTData = CreateFftData(signal, dataBlock.StartTime, Math.Min(sampleCount, FftSize));

                    cache.Add(dataBlockHash, resampledFFTData);

                    return resampledFFTData;
                }
            }
        }

        /// <summary>
        /// Creates FFT data from a given <see cref="DiscreteSignal"/>, applies filtering and resampling.
        /// </summary>
        /// <param name="signal">The input signal. It is expected that this signal has a length equal to <see cref="FftSize"/>.</param>
        /// <param name="startTime">The start time of the data block from which the signal was derived.</param>
        /// <param name="originalSampleCount">The number of original samples in the data block before any padding or truncation for FFT processing. This is used for metadata in <see cref="FftDataV3"/>.</param>
        /// <returns>An <see cref="FftDataV3"/> object containing the processed and resampled FFT data.</returns>
        /// <exception cref="Exception">Thrown if the provided signal's length is less than <see cref="FftSize"/> (this check is done by the underlying FFT algorithm if not explicitly here, but the input `signal` to `fft.MagnitudeSpectrum` must have at least `FftSize` samples).</exception>
        public FftDataV3 CreateFftData(DiscreteSignal signal, DateTimeOffset startTime, int originalSampleCount)
        {
            FftDataV3 fftData = new()
            {
                Start = startTime,
                End = startTime.AddSeconds(BlockLength), // Duration of the original data block
                BasedOnSamplesCount = originalSampleCount,
                FirstFrequency = 0, // DC component is usually ignored, actual first bin is Fs/N
                LastFrequency = (float)Samplerate / 2, // Nyquist frequency
                FftSize = FftSize,
            };

            //logger.LogInformation($"#{bi.ToString("0000")} signal.Samples.Length: {sampleCount:N0} | FFT size: {config.Postprocessing.FFTSize:N0}");

            // The NWaves RealFft expects the input signal to be of length FftSize.
            // If signal.Length > FftSize, it will take the first FftSize samples.
            // If signal.Length < FftSize, it would typically throw an error or produce incorrect results.
            // The Get() method already ensures the signal passed here has FftSize samples by padding or taking a subset.
            if (signal.Length < FftSize)
            {
                 // This case should ideally not be reached if Get() prepares the signal correctly.
                throw new ArgumentException($"Signal length ({signal.Length}) must be at least FFT size ({FftSize}).", nameof(signal));
            }

            // calculate magnitude spectrum with normalization and ignore the 0th coefficient (DC component)
            // The .Samples[1..] correctly skips the DC component.
            fftData.MagnitudeData = fft.MagnitudeSpectrum(signal, normalize: true).Samples[1..];

            // The number of bins in MagnitudeData will be FftSize / 2.
            // FrequencyStep is Nyquist / (Number of bins - 1) if we consider the bins themselves.
            // Or, more simply, Samplerate / FftSize for the raw FFT output.
            // Since we skipped DC, length is FftSize/2. Last actual frequency is Nyquist - (Samplerate/FftSize).
            // Effective number of points for frequency calculation after skipping DC is (FftSize / 2).
            fftData.FrequencyStep = fftData.LastFrequency / (fftData.MagnitudeData.Length);


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
