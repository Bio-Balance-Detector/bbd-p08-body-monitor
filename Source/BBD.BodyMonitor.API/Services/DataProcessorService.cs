using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.MLProfiles;
using BBD.BodyMonitor.Models;
using EDFCSharp;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;
using NWaves.Audio;
using NWaves.Signals;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace BBD.BodyMonitor.Services
{
    internal class DataProcessorService : IDataProcessorService
    {
        private readonly ILogger _logger;
        private BodyMonitorOptions _config;
        private readonly ISessionManagerService _sessionManager;
        private readonly float _inputAmplification = short.MaxValue / 1.0f;
        private DataAcquisition? dataAcquisition;
        private DataAcquisition? calibration;
        private FftDataV3? calibrationFftData;
        private FftDataBlockCache fftDataBlockCache;
        private FftDataV3? frequencyResponseAnalysisFftData;
        private int lastMaxIndex = 0;
        private readonly Random rnd = new();
        private readonly string[] audioFrameworks = { "dshow", "alsa", "openal", "oss" };
        private readonly string ffmpegAudioRecordingExtension = "wav";
        private readonly string ffmpegAudioRecordingParameters = "-c:a pcm_s16le -ac 1 -ar 44100";
        private readonly string ffmpegAudioEncodingExtension = "mp3";
        private readonly string ffmpegAudioEncodingParameters = "-c:a mp3 -ac 1 -ar 44100 -q:a 9 -filter:a loudnorm";
        private readonly float[] selfCalibrationAmplitudes = new float[] { 0.01f, 0.1f, 0.5f, 0.75f, 1.0f };
        private readonly float[] selfCalibrationFrequencies = new float[] { 0.1234f, 1.234f, 123.4f, 12340f, 123400f };
        private readonly int[] selfCalibrationFFTSizes = new int[] { 1 * 1024, 8 * 1024, 64 * 1024, 256 * 1024, 8192 * 1024 };
        private readonly DriveInfo? spaceCheckDrive;
        private readonly List<float> maxValues = new();
        private Pen[]? chartPens;
        private readonly List<string> disabledIndicators = new();

        [Obsolete]
        public DataProcessorService(ILogger<DataProcessorService> logger, IConfiguration configRoot, IOptionsMonitor<BodyMonitorOptions> bodyMonitorOptions, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _config = new BodyMonitorOptions(configRoot.GetSection("BodyMonitor"));
            _sessionManager = sessionManager;
            _sessionManager.SetDataDirectory(_config.DataDirectory);
            _inputAmplification = short.MaxValue / _config.DataWriter.OutputRange;

            //var bmo = bodyMonitorOptions.CurrentValue;

            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                if (Path.GetFullPath(AppendDataDir("")).ToLower().StartsWith(d.RootDirectory.FullName.ToLower()) && (spaceCheckDrive == null || d.Name.Length > spaceCheckDrive.Name.Length))
                {
                    spaceCheckDrive = d;
                }
            }

            fftDataBlockCache = new FftDataBlockCache(_config.Acquisition.Samplerate, _config.Postprocessing.FFTSize, _config.Postprocessing.DataBlock, _config.Postprocessing.ResampleFFTResolutionToHz);

            _ = GetFFMPEGAudioDevices();

            //ProcessCommandLineArguments(args);
        }

        [Obsolete]
        public void PreprareMachineLearningModels()
        {
            _logger.LogInformation("Preparing machine learning models, please wait...");
            FftDataV3 dummnyFftData = new()
            {
                FirstFrequency = 0,
                LastFrequency = float.MaxValue,
                FftSize = _config.Postprocessing.FFTSize,
                MagnitudeData = new float[_config.Postprocessing.FFTSize]
            };
            _ = EvaluateIndicators(_logger, 0, dummnyFftData);
        }

        public ConnectedDevice[] ListDevices()
        {
            dataAcquisition ??= new DataAcquisition(_logger);
            return dataAcquisition.ListDevices();
        }

        [Obsolete]
        public string? StartDataAcquisition(string deviceSerialNumber)
        {
            System.Timers.Timer checkDiskSpaceTimer = new(10000);
            checkDiskSpaceTimer.Elapsed += CheckDiskSpaceTimer_Elapsed;
            checkDiskSpaceTimer.AutoReset = true;
            checkDiskSpaceTimer.Enabled = true;
            int deviceIndex = GetDeviceIndexFromSerialNumber(deviceSerialNumber);
            if (deviceIndex == -1)
            {
                _logger.LogWarning($"Device with serial number '{deviceSerialNumber}' not found, using default device.");
            }

            try
            {
                dataAcquisition ??= new DataAcquisition(_logger);
                deviceSerialNumber = dataAcquisition.OpenDevice(deviceIndex, _config.Acquisition.Channels, _config.Acquisition.Samplerate, _config.SignalGenerator.Enabled, _config.SignalGenerator.Channel, _config.SignalGenerator.Frequency, _config.SignalGenerator.Voltage, _config.Acquisition.Block, _config.Acquisition.Buffer);
                dataAcquisition.BufferError += DataAcquisition_BufferError;

                if (_config.DataWriter.Enabled)
                {
                    dataAcquisition.SubscribeToBlockReceived(_config.DataWriter.Interval, DataWriter_BlockReceived);
                }
                if (_config.Postprocessing.Enabled)
                {
                    dataAcquisition.SubscribeToBlockReceived(_config.Postprocessing.Interval, Postprocessing_BlockReceived);
                }
                if (_config.AudioRecording.Enabled)
                {
                    dataAcquisition.SubscribeToBlockReceived(_config.AudioRecording.Interval, AudioRecording_BlockReceived);
                }
                if (_config.Indicators.Enabled)
                {
                    dataAcquisition.SubscribeToBlockReceived(_config.Indicators.Interval, Indicators_BlockReceived);
                }

                _logger.LogInformation($"Recording data on '{dataAcquisition.SerialNumber}' at {SimplifyNumber(dataAcquisition.Samplerate)}Hz" + (_config.Postprocessing.Enabled ? $" with the effective FFT resolution of {dataAcquisition.Samplerate / 2.0 / (_config.Postprocessing.FFTSize / 2):0.00} Hz" : "") + "...");
                dataAcquisition.Start();
            }
            catch (Exception)
            {
                return null;
            }

            return deviceSerialNumber;
        }

        public bool StopDataAcquisition(string deviceSerialNumber)
        {
            if (dataAcquisition == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                // get the serail number of the default device
                deviceSerialNumber = dataAcquisition.SerialNumber;
            }

            if (dataAcquisition.DeviceIndex != GetDeviceIndexFromSerialNumber(deviceSerialNumber))
            {
                return false;
            }

            dataAcquisition.Stop();

            _logger.LogInformation($"Finished recording data on '{dataAcquisition.SerialNumber}' at {SimplifyNumber(dataAcquisition.Samplerate)}Hz" + (_config.Postprocessing.Enabled ? $" with the effective FFT resolution of {dataAcquisition.Samplerate / 2.0 / (_config.Postprocessing.FFTSize / 2):0.00} Hz" : "") + ".");

            return true;
        }

        private void DataAcquisition_BufferError(object sender, BufferErrorEventArgs e)
        {
            _ = Task.Run(() =>
            {
                //_logger.LogWarning($"Data error! cAvailable: {e.BytesAvailable / (float)e.BytesTotal:P}, cLost: {e.BytesLost / (float)e.BytesTotal:P}, cCorrupted: {e.BytesCorrupted / (float)e.BytesTotal:P}");
                _ = _sessionManager.ResetSession();
            });
        }

        public void CalibrateDevice(string deviceSerialNumber)
        {
            int deviceIndex = GetDeviceIndexFromSerialNumber(deviceSerialNumber);

            calibration = new DataAcquisition(_logger);
            calibration.SubscribeToBlockReceived(1.0f, FrequencyResponseAnalysis_SamplesReceived);

            int samplerate = 1 * 1000 * 1000;
            float signalFrequency = selfCalibrationFrequencies[2];
            float signalAmplitude = selfCalibrationAmplitudes[2];
            int fftSize = selfCalibrationFFTSizes[2];
            _logger.LogInformation($"Starting calibration with the default values of {samplerate} SPS samplerate, {signalFrequency} Hz, {signalAmplitude} V signal and {fftSize} FFT size.");

            float[] amplitudeErrors = new float[5];
            float[] frequencyErrors = new float[5];

            for (int ai = 0; ai < selfCalibrationAmplitudes.Length; ai++)
            {
                signalAmplitude = selfCalibrationAmplitudes[ai];

                _ = calibration.OpenDevice(deviceIndex, new int[] { 1 }, samplerate, true, 2, signalFrequency, signalAmplitude, 0.1f, 1.0f);
                calibration.BufferSize = fftSize * 2;

                calibrationFftData = new FftDataV3()
                {
                    Start = DateTimeOffset.Now,
                    FirstFrequency = 0,
                    LastFrequency = calibration.Samplerate / 2,
                    FftSize = fftSize,
                };

                calibration.Start();
                calibration.CloseDevice();

                float maxMagnitude = calibrationFftData.MagnitudeData.Max();
                int maxIndex = Array.FindIndex(calibrationFftData.MagnitudeData, md => md == maxMagnitude);
                frequencyErrors[ai] = (calibrationFftData.FirstFrequency + (maxIndex * calibrationFftData.FrequencyStep) - signalFrequency) * 1000;
                amplitudeErrors[ai] = (maxMagnitude - signalAmplitude) * 1000;
            }
            _logger.LogInformation($"Errors are ({string.Join(" | ", amplitudeErrors.Select(e => e.ToString("0.0").PadLeft(7)))}) mV and ({string.Join(" | ", frequencyErrors.Select(e => e.ToString("0.0").PadLeft(9)))}) mHz at ({string.Join(" | ", selfCalibrationAmplitudes.Select(a => a.ToString().PadLeft(7)))}) V amplitudes respectively.");

            signalAmplitude = selfCalibrationAmplitudes[2];
            for (int fi = 0; fi < selfCalibrationFrequencies.Length; fi++)
            {
                signalFrequency = selfCalibrationFrequencies[fi];

                _ = calibration.OpenDevice(deviceIndex, new int[] { 1 }, samplerate, true, 2, signalFrequency, signalAmplitude, 0.1f, 1.0f);
                calibration.BufferSize = fftSize * 2;

                calibrationFftData = new FftDataV3()
                {
                    Start = DateTimeOffset.Now,
                    FirstFrequency = 0,
                    LastFrequency = calibration.Samplerate / 2,
                    FftSize = fftSize,
                };

                calibration.Start();
                calibration.CloseDevice();

                float maxMagnitude = calibrationFftData.MagnitudeData.Max();
                int maxIndex = Array.FindIndex(calibrationFftData.MagnitudeData, md => md == maxMagnitude);
                frequencyErrors[fi] = (calibrationFftData.FirstFrequency + (maxIndex * calibrationFftData.FrequencyStep) - signalFrequency) * 1000;
                amplitudeErrors[fi] = (maxMagnitude - signalAmplitude) * 1000;
            }
            _logger.LogInformation($"Errors are ({string.Join(" | ", amplitudeErrors.Select(e => e.ToString("0.0").PadLeft(7)))}) mV and ({string.Join(" | ", frequencyErrors.Select(e => e.ToString("0.0").PadLeft(9)))}) mHz at ({string.Join(" | ", selfCalibrationFrequencies.Select(a => a.ToString().PadLeft(7)))}) Hz respectively.");

            signalFrequency = selfCalibrationFrequencies[2];
            for (int si = 0; si < selfCalibrationFFTSizes.Length; si++)
            {
                fftSize = selfCalibrationFFTSizes[si];

                _ = calibration.OpenDevice(deviceIndex, new int[] { 1 }, samplerate, true, 2, signalFrequency, signalAmplitude, 0.1f, 1.0f);
                calibration.BufferSize = fftSize * 2;

                calibrationFftData = new FftDataV3()
                {
                    Start = DateTimeOffset.Now,
                    FirstFrequency = 0,
                    LastFrequency = calibration.Samplerate / 2,
                    FftSize = fftSize,
                };

                calibration.Start();
                calibration.CloseDevice();

                float maxMagnitude = calibrationFftData.MagnitudeData.Max();
                int maxIndex = Array.FindIndex(calibrationFftData.MagnitudeData, md => md == maxMagnitude);
                frequencyErrors[si] = (calibrationFftData.FirstFrequency + (maxIndex * calibrationFftData.FrequencyStep) - signalFrequency) * 1000;
                amplitudeErrors[si] = (maxMagnitude - signalAmplitude) * 1000;
            }
            _logger.LogInformation($"Errors are ({string.Join(" | ", amplitudeErrors.Select(e => e.ToString("0.0").PadLeft(7)))}) mV and ({string.Join(" | ", frequencyErrors.Select(e => e.ToString("0.0").PadLeft(9)))}) mHz at ({string.Join(" | ", selfCalibrationFFTSizes.Select(a => a.ToString().PadLeft(7)))}) FFT sizes respectively.");
        }

        public void FrequencyResponseAnalysis(string deviceSerialNumber)
        {
            if (!_config.Acquisition.Enabled || !_config.SignalGenerator.Enabled)
            {
                throw new Exception("Acquisition and signal generator must be enabled for the frequnecy response analysis.");
            }

            DataAcquisition frada = new(_logger);
            int deviceIndex = GetDeviceIndexFromSerialNumber(deviceSerialNumber);

            FrequencyAnalysisSettings frequencyAnalysisSettings = new()
            {
                StartFrequency = 1 / _config.Acquisition.Block,
                EndFrequency = _config.Acquisition.Samplerate / 2,
                FrequencyStep = _config.Postprocessing.ResampleFFTResolutionToHz,
                Samplerate = _config.Acquisition.Samplerate,
                FftSize = _config.Postprocessing.FFTSize,
                BlockLength = _config.Acquisition.Block
            };

            int frSampleNumber = (int)(frequencyAnalysisSettings.Samplerate / 2 / frequencyAnalysisSettings.FrequencyStep);
            float[] result = new float[frSampleNumber];


            //float minimumDurationParFrequency = ((int)(((float)frequencyAnalysisSettings.FftSize / frequencyAnalysisSettings.Samplerate) / frequencyAnalysisSettings.BlockLength) + 1) * frequencyAnalysisSettings.BlockLength;
            //if (minimumDurationParFrequency > 0.02)
            //{
            //    // the whole test would take too much time, so we need to switch to a lower samplerate if we want to keep our FFT resoltion
            //    float sampleRateReductionRate = minimumDurationParFrequency / 0.02f;
            //    frada.OpenDevice(_config.Acquisition.Channels, _config.Acquisition.Samplerate / sampleRateReductionRate, _config.SignalGenerator.Enabled, _config.SignalGenerator.Channel, _config.SignalGenerator.Frequency, _config.SignalGenerator.Voltage, _config.Acquisition.Block, _config.Acquisition.Buffer);
            //    frequencyAnalysisSettings.Samplerate = frada.Samplerate;
            //    frada.CloseDevice();
            //    frequencyAnalysisSettings.EndFrequency = (int)frequencyAnalysisSettings.Samplerate / 2;

            //    double newFftSize = _config.Postprocessing.FFTSize / sampleRateReductionRate;
            //    double newFftSquareRoot = Math.Log2(newFftSize);
            //    frequencyAnalysisSettings.FftSize = (int)Math.Pow(2, Math.Ceiling(newFftSquareRoot));
            //}

            while (frequencyAnalysisSettings.Samplerate <= _config.Acquisition.Samplerate)
            {
                float currentFftFrequencyStep = frequencyAnalysisSettings.Samplerate / frequencyAnalysisSettings.FftSize;
                _logger.LogInformation($"The frequency response analysis between {frequencyAnalysisSettings.StartFrequency} and {frequencyAnalysisSettings.EndFrequency} Hz will be performed with a sampling rate of {frequencyAnalysisSettings.Samplerate} Hz and a FFT size of {frequencyAnalysisSettings.FftSize} ({currentFftFrequencyStep} Hz per bin).");

                RunPartialFrequencyAnalysis(deviceIndex, frada, frequencyAnalysisSettings, result);

                frequencyAnalysisSettings.Samplerate *= 2;
                _ = frada.OpenDevice(deviceIndex, _config.Acquisition.Channels, frequencyAnalysisSettings.Samplerate, _config.SignalGenerator.Enabled, _config.SignalGenerator.Channel, _config.SignalGenerator.Frequency, _config.SignalGenerator.Voltage, _config.Acquisition.Block, _config.Acquisition.Buffer);
                frequencyAnalysisSettings.Samplerate = frada.Samplerate;
                frada.CloseDevice();
                frequencyAnalysisSettings.StartFrequency = frequencyAnalysisSettings.EndFrequency;
                frequencyAnalysisSettings.EndFrequency = (int)frequencyAnalysisSettings.Samplerate / 2;
                frequencyAnalysisSettings.FftSize *= 2;
            }

            if (frequencyResponseAnalysisFftData != null)
            {
                string pathToFile = GeneratePathToFile(DateTimeOffset.Now.ToUniversalTime().DateTime);
                string fraFilename = AppendDataDir($"{pathToFile}__FRA.csv");

                // normalize the result data
                float maxResult = result.Max();
                result = result.Select(r => r / maxResult).ToArray();

                // save the result data
                string[] columnNames = Enumerable.Range(0, frSampleNumber).Select(i => "Freq_" + frequencyResponseAnalysisFftData.GetBinFromIndex(i).ToString("0.00").Replace(".", "p") + "_Hz").ToArray();
                File.WriteAllText(fraFilename, string.Join(",", columnNames) + System.Environment.NewLine);
                File.AppendAllText(fraFilename, string.Join(",", result.Select(md => md.ToString("0.0000000000", System.Globalization.CultureInfo.InvariantCulture.NumberFormat))));
            }
        }

        private void RunPartialFrequencyAnalysis(int deviceIndex, DataAcquisition frada, FrequencyAnalysisSettings frequencyAnalysisSettings, float[] result)
        {
            fftDataBlockCache = new FftDataBlockCache((int)frequencyAnalysisSettings.Samplerate, frequencyAnalysisSettings.FftSize, frequencyAnalysisSettings.BlockLength, frequencyAnalysisSettings.FrequencyStep);

            _ = frada.OpenDevice(deviceIndex, _config.Acquisition.Channels, frequencyAnalysisSettings.Samplerate, _config.SignalGenerator.Enabled, _config.SignalGenerator.Channel, _config.SignalGenerator.Frequency, _config.SignalGenerator.Voltage, frequencyAnalysisSettings.BlockLength, _config.Acquisition.Buffer);
            //frada.SubscribeToBlockReceived(frequencyAnalysisSettings.FftSize / frequencyAnalysisSettings.Samplerate, FrequencyResponseAnalysis_SamplesReceived);
            frada.SubscribeToBlockReceived(0.05f, FrequencyResponseAnalysis_SamplesReceived);
            fftDataBlockCache = new FftDataBlockCache((int)frequencyAnalysisSettings.Samplerate, frequencyAnalysisSettings.FftSize, frequencyAnalysisSettings.BlockLength, frequencyAnalysisSettings.FrequencyStep);

            _ = Task.Run(frada.Start);

            frada.SweepSingalGeneratorFrequency(frequencyAnalysisSettings.StartFrequency, frequencyAnalysisSettings.EndFrequency, 45.0f);
            //frada.ChangeSingalGeneratorFrequency(frequencyAnalysisSettings.EndFrequency);

            DateTime endTime = DateTime.UtcNow.AddSeconds(15.0);
            lastMaxIndex = 0;
            //for (int i = (int)(frequencyAnalysisSettings.StartFrequency / frequencyAnalysisSettings.FrequencyStep); i < frequencyAnalysisSettings.EndFrequency / frequencyAnalysisSettings.FrequencyStep; i++)
            while (DateTime.UtcNow < endTime)
            {
                //float frequencyToTest = ((i + 0.5f) * frequencyAnalysisSettings.FrequencyStep);
                //frada.ChangeSingalGeneratorFrequency(frequencyToTest);
                //frada.ClearBuffer();
                float valueMultiplier = 1 / _config.SignalGenerator.Voltage / _config.Postprocessing.FFTSize * 200000000;

                frequencyResponseAnalysisFftData = null;
                while (frequencyResponseAnalysisFftData == null) { }
                //result[i] = Math.Max(frequencyResponseAnalysisFftData.MagnitudeData[i], Math.Max(frequencyResponseAnalysisFftData.MagnitudeData[i - 1], frequencyResponseAnalysisFftData.MagnitudeData[i + 1])) * valueMultiplier;

                //int maxIndex = Array.IndexOf(frequencyResponseAnalysisFftData.MagnitudeData, frequencyResponseAnalysisFftData.MagnitudeData.Max());
                float[] magnitudeData = frequencyResponseAnalysisFftData.MagnitudeData.TakeLast(frequencyResponseAnalysisFftData.MagnitudeData.Length - lastMaxIndex).ToArray();
                int maxIndex = Array.IndexOf(magnitudeData, magnitudeData.Max());
                float maxValue = frequencyResponseAnalysisFftData.MagnitudeData[lastMaxIndex + maxIndex] * valueMultiplier;
                float maxFreq = frequencyResponseAnalysisFftData.GetBinFromIndex(lastMaxIndex + maxIndex).MiddleFrequency;

                //if (frequencyToTest != maxFreq)
                //{
                //    _logger.LogWarning($"{result[i]:0.00000} @ ~{frequencyToTest:0.00} Hz <> {maxValue:0.00000} @ {maxIndex} ~{maxFreq:0.00} Hz");
                //}
                //else
                //{
                //    _logger.LogInformation($"{result[i]:0.00000} @ ~{frequencyToTest:0.00} Hz == {maxValue:0.00000} @ {maxIndex} ~{maxFreq:0.00} Hz");
                //}

                _logger.LogInformation($"{frada.GetSignalGeneratorFrequency():0.00} Hz -> {maxValue:0.00000} @ {lastMaxIndex + maxIndex,7} ~{maxFreq,10:0.00} Hz");

                result[(int)(maxFreq / frequencyAnalysisSettings.FrequencyStep)] = maxValue;

                //lastMaxIndex += maxIndex;
            }

            frada.Stop();
            frada.CloseDevice();
        }

        public void FrequencyResponseAnalysis_SamplesReceived(object sender, BlockReceivedEventArgs e)
        {
            if (frequencyResponseAnalysisFftData != null)
            {
                return;
            }

            //float minimumDuration = ((int)(((float)frequencyAnalysisSettings.FFTSize / frequencyAnalysisSettings.Samplerate) / frequencyAnalysisSettings.Block) + 1) * frequencyAnalysisSettings.Block;
            //DataBlock dataBlock = e.Buffer.GetBlocks(minimumDuration, e.DataBlock.EndIndex);
            DataBlock dataBlock = e.DataBlock;

            //if (dataBlock.Data.Length < _config.Postprocessing.FFTSize)
            //{
            //    return;
            //}

            try
            {
                // apply fade in to the buffer to make the previous signals less significant
                if (dataBlock.Data.Length > 1)
                {
                    for (int i = dataBlock.Data.Length - 1; i >= 0; i--)
                    {
                        float multiplier = (float)i / (dataBlock.Data.Length - 1);
                        dataBlock.Data[i] *= multiplier;
                    }
                }

                frequencyResponseAnalysisFftData = fftDataBlockCache.Get(dataBlock, FftFillMethod.ZeroFill);

                string pathToFile = GeneratePathToFile(DateTimeOffset.Now.ToUniversalTime().DateTime);

                FileStream waveFileStream = new(AppendDataDir($"{pathToFile}_{rnd.Next(999999):000000}__{_config.Acquisition.Samplerate}sps_ch{_config.Acquisition.Channels[0]}.wav"), FileMode.Create);
                DiscreteSignal signalToSave = new(_config.Acquisition.Samplerate, e.DataBlock.Data, true);
                signalToSave.Amplify(_inputAmplification);
                WaveFile waveFile = new(signalToSave, 16);
                waveFile.SaveTo(waveFileStream, false);

                waveFileStream.Close();
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogError($"The FFT size of {_config.Postprocessing.FFTSize:N0} is too high for the sample rate of {_config.Acquisition.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                return;
            }
        }

        public void CheckDiskSpaceTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (spaceCheckDrive != null && spaceCheckDrive.AvailableFreeSpace < _config.MinimumAvailableFreeSpace)
            {
                _logger.LogError($"There is not enough space on drive {spaceCheckDrive.Name}.  to continue. There must be at least {_config.MinimumAvailableFreeSpace:N0} bytes of available free space at all times.");
                dataAcquisition?.Stop();
            }
        }

        public void DataWriter_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            if (dataAcquisition == null)
            {
                return;
            }

            string waveFilename;
            if (_config.DataWriter.SingleFile)
            {
                Sessions.Session session = _sessionManager.GetSession(null);
                if ((session == null) || (!session.StartedAt.HasValue))
                {
                    return;
                }
                string pathToFile = GeneratePathToFile(session.StartedAt.Value.UtcDateTime);
                waveFilename = $"{pathToFile}__{session.Alias}_{_config.Acquisition.Samplerate}sps_ch{_config.Acquisition.Channels[0]}.wav";
            }
            else
            {
                string pathToFile = GeneratePathToFile(e.DataBlock.StartTime.ToUniversalTime().DateTime);
                waveFilename = $"{pathToFile}__{_config.Acquisition.Samplerate}sps_ch{_config.Acquisition.Channels[0]}.wav";
            }
            waveFilename = AppendDataDir(waveFilename);

            _ = Task.Run(() =>
            {
                string threadId = e.DataBlock.EndIndex.ToString("0000DW");
                _logger.LogTrace($"Data writer thread #{threadId} begin");

                Stopwatch sw = Stopwatch.StartNew();

                if (_config.DataWriter.SaveAsWAV)
                {
                    sw.Restart();
                    // and save samples to a WAV file
                    FileStream waveFileStream = new(waveFilename, FileMode.OpenOrCreate);
                    DiscreteSignal signalToSave = new((int)dataAcquisition.Samplerate, e.DataBlock.Data, true);
                    signalToSave.Amplify(_inputAmplification);

                    // force the values to fit into the 16-bit range
                    signalToSave = new DiscreteSignal((int)dataAcquisition.Samplerate, signalToSave.Samples.Select(v => v < short.MinValue ? short.MinValue : v > short.MaxValue ? short.MaxValue : v).ToArray(), true);

                    float minSignal = signalToSave.Samples.Min();
                    float maxSignal = signalToSave.Samples.Max();

                    if ((minSignal == short.MinValue) || (maxSignal == short.MaxValue))
                    {
                        _logger.LogWarning("The samples in the WAV file are clipped. This is usually caused by the input signal being too strong.");
                    }

                    float maxAbsSignal = (float)Math.Max(-minSignal, maxSignal);
                    int bitsLost = 0;
                    while (maxAbsSignal <= short.MaxValue)
                    {
                        maxAbsSignal *= 2;
                        bitsLost++;
                    }

                    if (bitsLost > 4)
                    {
                        _logger.LogWarning($"The effective bitrate of the WAV file is less than {16 - bitsLost + 1} bits. This is usually caused by a weak input signal. Consider lowering the output range in the configuration file.");
                    }

                    if (_config.DataWriter.SingleFile)
                    {
                        WaveFileExtensions.AppendTo(waveFileStream, signalToSave);
                    }
                    else
                    {
                        WaveFile waveFile = new(signalToSave, 16);
                        waveFile.SaveTo(waveFileStream, false);
                    }

                    waveFileStream.Close();

                    sw.Stop();
                    _logger.LogTrace($"#{threadId} Save as WAV completed in {sw.ElapsedMilliseconds:N0} ms.");
                }

                _logger.LogTrace($"Data writer thread #{threadId} end");
            }
            );
        }

        [Obsolete]
        public void Postprocessing_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            if (dataAcquisition == null)
            {
                return;
            }

            DataBlock dataBlock = e.Buffer.GetBlocks(_config.Postprocessing.DataBlock, e.DataBlock.EndIndex);
            string pathToFile = GeneratePathToFile(dataBlock.StartTime.ToUniversalTime().DateTime);

            if (_config.Postprocessing.Enabled)
            {
                _ = Task.Run(() =>
                {
                    string threadId = dataBlock.EndIndex.ToString("0000PP");
                    _logger.LogTrace($"Postprocessing thread #{threadId} begin");

                    Stopwatch sw = Stopwatch.StartNew();
                    sw.Restart();

                    FftDataV3? resampledFFTData = null;
                    try
                    {
                        resampledFFTData = fftDataBlockCache.Get(dataBlock);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        _logger.LogError($"The FFT size of {_config.Postprocessing.FFTSize:N0} is too high for the sample rate of {_config.Acquisition.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                        dataAcquisition.Stop();
                        return;
                    }

                    if (resampledFFTData == null)
                    {
                        return;
                    }

                    sw.Stop();
                    _logger.LogTrace($"#{threadId} Signal processing completed in {sw.ElapsedMilliseconds:N0} ms.");

                    if (_config.SignalGenerator.Enabled)
                    {
                        int fftDataIndex = (int)((_config.SignalGenerator.Frequency - resampledFFTData.FirstFrequency) / resampledFFTData.FrequencyStep);
                        int fftDataIndexStart = fftDataIndex - 2;
                        int fftDataIndexEnd = fftDataIndex + 2 + 1;

                        if (fftDataIndex - 1 < 0 || fftDataIndex + 1 > resampledFFTData.MagnitudeData.Length - 1)
                        {
                            _logger.LogWarning($"#{threadId} The signal generator generates a signal at {_config.SignalGenerator.Frequency:N} Hz and that is out of the range of the current sampling ({resampledFFTData.FirstFrequency:N} Hz - {resampledFFTData.LastFrequency:N} Hz).");
                        }
                        else
                        {
                            _logger.LogInformation($"#{threadId} Normalized magnitude values around {_config.SignalGenerator.Frequency} Hz are ({string.Join(" | ", resampledFFTData.MagnitudeData[fftDataIndexStart..fftDataIndexEnd].Select(m => string.Format("{0,7:N}", m * 1000 * 1000)))}) µV.");
                        }
                    }

                    string fftRange = resampledFFTData.GetFFTRange();
                    if (_config.Postprocessing.SaveAsFFT)
                    {
                        _ = Task.Run(() =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();

                            //save the FFT to a JSON file
                            sw.Restart();
                            FftDataV3.SaveAsJson(resampledFFTData, AppendDataDir($"{pathToFile}__{fftRange}.fft"), false);
                            sw.Stop();
                            _logger.LogTrace($"#{threadId} Save as FFT completed in {sw.ElapsedMilliseconds:N0} ms.");
                        });
                    }

                    if (_config.Postprocessing.SaveAsCompressedFFT)
                    {
                        _ = Task.Run(() =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();

                            //save the FFT to a zipped JSON file
                            sw.Restart();
                            FftDataV3.SaveAsJson(resampledFFTData, AppendDataDir($"{pathToFile}__{fftRange}.zip"), true);
                            sw.Stop();
                            _logger.LogTrace($"#{threadId} Save as compressed FFT completed in {sw.ElapsedMilliseconds:N0} ms.");
                        });
                    }

                    if (_config.Postprocessing.SaveAsBinaryFFT)
                    {
                        _ = Task.Run(() =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();

                            //save the FFT to a binary file
                            sw.Restart();
                            FftDataV3.SaveAsBinary(resampledFFTData, AppendDataDir($"{pathToFile}__{fftRange}.fft"), false);
                            sw.Stop();
                            _logger.LogTrace($"#{threadId} Save as binary FFT completed in {sw.ElapsedMilliseconds:N0} ms.");
                        });
                    }


                    if (_config.Postprocessing.SaveAsPNG.Enabled)
                    {
                        MLProfile? mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => !string.IsNullOrWhiteSpace(_config.Postprocessing.SaveAsPNG.MLProfile) && p.Name.StartsWith(_config.Postprocessing.SaveAsPNG.MLProfile));
                        if (mlProfile == null)
                        {
                            _logger.LogError($"The profile '{_config.Postprocessing.SaveAsPNG.MLProfile}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                            _config.Postprocessing.SaveAsPNG.Enabled = false;
                        }
                        else
                        {
                            _ = Task.Run(() =>
                            {
                                Stopwatch sw = Stopwatch.StartNew();

                                //save a PNG with the values
                                sw.Restart();
                                string filenameComplete = $"{pathToFile}_{SimplifyNumber(mlProfile.MaxFrequency)}Hz_{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY.Min)}-{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY.Max)}{_config.Postprocessing.SaveAsPNG.RangeY.Unit}.png";
                                SaveSignalAsPng(AppendDataDir(filenameComplete), resampledFFTData, _config.Postprocessing.SaveAsPNG, mlProfile);
                                sw.Stop();
                                _logger.LogTrace($"#{threadId} Save as PNG completed in {sw.ElapsedMilliseconds:N0} ms.");
                            });
                        }
                    }

                    _logger.LogTrace($"Postprocessing thread #{threadId} end");
                }
                );
            }
        }

        [Obsolete]
        public void Indicators_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            if (dataAcquisition == null)
            {
                return;
            }

            DataBlock dataBlock = e.Buffer.GetBlocks(_config.Postprocessing.DataBlock, e.DataBlock.EndIndex);
            string pathToFile = GeneratePathToFile(e.DataBlock.StartTime.ToUniversalTime().DateTime);

            if (_config.Indicators.Enabled)
            {
                _ = Task.Run(() =>
                {
                    string threadId = e.DataBlock.EndIndex.ToString("0000IN");
                    _logger.LogTrace($"Indicators thread #{threadId} begin");

                    Stopwatch sw = Stopwatch.StartNew();

                    FftDataV3? resampledFFTData = null;
                    try
                    {
                        resampledFFTData = fftDataBlockCache.Get(dataBlock);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        _logger.LogError($"The FFT size of {_config.Postprocessing.FFTSize:N0} is too high for the sample rate of {_config.Acquisition.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                        dataAcquisition.Stop();
                        return;
                    }

                    if (resampledFFTData == null)
                    {
                        return;
                    }

                    if (_config.Indicators.Enabled)
                    {
                        IndicatorEvaluationResult[] evaluationResults = EvaluateIndicators(_logger, dataBlock.EndIndex, resampledFFTData);

                        if (evaluationResults.Length > 0)
                        {
                            _logger.LogInformation($"#{threadId} {resampledFFTData.Start:HH:mm:ss}-{resampledFFTData.Start?.AddSeconds(_config.Postprocessing.Interval):HH:mm:ss} {string.Join(" | ", evaluationResults.Select(er => er.DisplayText + " " + er.PredictionScore.ToString("+0.00;-0.00; 0.00").PadLeft(7)))}.");
                        }
                    }

                    _logger.LogTrace($"Indicators thread #{threadId} end");
                }
                );
            }
        }

        public void AudioRecording_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            DateTime captureTime = DateTime.UtcNow.AddSeconds(-_config.Postprocessing.Interval);
            string pathToFile = GeneratePathToFile(captureTime);

            if (_config.AudioRecording.Enabled)
            {
                _ = Task.Run(() =>
                {
                    TimeSpan tp = captureTime.Add(new TimeSpan(0, 0, (int)_config.Postprocessing.Interval * 2)) - DateTime.UtcNow;

                    string pathToAudioFile = GeneratePathToFile(DateTime.UtcNow);

                    string recFilename = AppendDataDir($"{pathToAudioFile}.{ffmpegAudioRecordingExtension}");

                    string ffmpegAudioFramework = _config.AudioRecording.PreferredDevice.Split("/")[0];
                    string ffmpegAudioDevice = _config.AudioRecording.PreferredDevice.Split("/")[1];
                    string audioRecordingCommandLine = $"-f {ffmpegAudioFramework} -ac 1 -i audio=\"{ffmpegAudioDevice}\" {ffmpegAudioRecordingParameters} -t {tp:mm':'ss'.'fff} \"{recFilename}\"";
                    _logger.LogDebug($"ffmpeg {audioRecordingCommandLine}");

                    try
                    {
                        _ = FFmpeg.Conversions.New().Start(audioRecordingCommandLine)
                            .ContinueWith((cr) =>
                            {
                                try
                                {
                                    Stopwatch sw = Stopwatch.StartNew();
                                    sw.Restart();

                                    float waveRms = 0;
                                    float wavePeak = 0;
                                    using (FileStream waveStream = new(recFilename, FileMode.Open))
                                    {
                                        DiscreteSignal recordedAudio = new WaveFile(waveStream).Signals[0];
                                        waveRms = recordedAudio.Rms();
                                        wavePeak = Math.Max(-recordedAudio.Samples.Min(), recordedAudio.Samples.Max()) * 100;
                                    }

                                    //if (waveRms >= config.AudioRecording.SilenceThreshold)
                                    if (wavePeak >= _config.AudioRecording.SilenceThreshold)
                                    {
                                        string finalFilename = AppendDataDir($"{pathToAudioFile}_{wavePeak:00.0}%.{ffmpegAudioEncodingExtension}");
                                        string audioEncodingCommandLine = $"-i {recFilename} {ffmpegAudioEncodingParameters} \"{finalFilename}\"";

                                        _logger.LogDebug($"ffmpeg {audioEncodingCommandLine}");
                                        FFmpeg.Conversions.New().Start(audioEncodingCommandLine).Wait();
                                    }

                                    File.Delete(recFilename);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning($"There was an error while encoding audio.");
                                    _logger.LogDebug($"{ex.Message.Split(System.Environment.NewLine)[^1]}");
                                }
                            });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"There was an error while recording audio.");
                        _logger.LogDebug($"{ex.Message.Split(System.Environment.NewLine)[^1]}");
                    }
                });
            }
        }

        public List<string> GetFFMPEGAudioDevices()
        {
            List<string> ffmpegAudioNames = new();

            ffmpegAudioNames.Clear();
            foreach (string audioFramework in audioFrameworks)
            {
                try
                {
                    string listDevicesCommandLine = $"-list_devices true -f {audioFramework} -i dummy";
                    _logger.LogDebug($"ffmpeg {listDevicesCommandLine}");
                    IConversion listDevicesConversion = FFmpeg.Conversions.New();
                    //listDevicesConversion.OnDataReceived += ListDevicesConversion_OnDataReceived;
                    listDevicesConversion.Start(listDevicesCommandLine).Wait();
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("ffmpeg"))
                    {
                        lock (ffmpegAudioNames)
                        {
                            foreach (string audioDeviceDetails in e.Message.Split(System.Environment.NewLine).Where(ol => ol.Trim().StartsWith("[")))
                            {
                                string listedAudioFramework = audioDeviceDetails.Split(new char[] { '[', '@' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                                string listedAudioDevice = audioDeviceDetails.Split(new char[] { ']', '\"' }, StringSplitOptions.RemoveEmptyEntries)[^1].Trim();

                                if (audioDeviceDetails.Contains("Alternative name") || !audioDeviceDetails.Contains("\""))
                                {
                                    continue;
                                }

                                ffmpegAudioNames.Add($"{listedAudioFramework}/{listedAudioDevice}");
                            }
                        }
                    }
                    else
                    {
                        _logger.LogError($"There was an error while enumerating ffmpeg audio devices in the '{audioFramework}' audio framework: '{e.Message}'");
                    }
                }
            }


            if (_config.AudioRecording.PreferredDevice.Split("/").Length != 2)
            {
                _logger.LogWarning($"The preferred audio input device in the config file is not in the correct format. It should be in the '<ffmpeg audio framework>/<ffmpeg audio device>' format. Audio will not be recorded.");
                _config.AudioRecording.Enabled = false;
            }

            if (_config.AudioRecording.Enabled && !ffmpegAudioNames.Contains(_config.AudioRecording.PreferredDevice))
            {
                _logger.LogWarning($"The preferred audio input device was not listed by the operating system as a valid audio source. The listed audio inputs are: {string.Join(", ", ffmpegAudioNames)}.");
            }

            return ffmpegAudioNames;
        }

        [Obsolete]
        public IndicatorEvaluationResult[] EvaluateIndicators(ILogger? logger, long blockIndex, FftDataV3 inputData)
        {
            List<IndicatorEvaluationResult> result = new();

            IndicatorEvaluationTaskDescriptor[] taskDescriptors = new IndicatorEvaluationTaskDescriptor[]
            {
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 0,
                    IndicatorName = "IsSubject_None",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20221103_2022-11-02_MLP14_0p25Hz-6250Hz_IsSubject_None_3710rows_#14_1,0000.zip",
                    DisplayText = "Not attached (MLP14-R1)?"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 1,
                    IndicatorName = "Session_SegmentedData_Sleep_Level",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20221106__TrainingData.Sleep__MLP14_0p25Hz-6250Hz__Session_SegmentedData_Sleep_Level__15372rows__#11_0,4839.zip",
                    DisplayText = "Sleep stage (MLP14-R2):"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 2,
                    IndicatorName = "Session_SegmentedData_Sleep_Level",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP15")),
                    MLModelFilename = "BBD_20221106__TrainingData.Sleep.MLP15__MLP15_1p00Hz-25000Hz__Session_SegmentedData_Sleep_Level__15372rows__#08_0,5312.zip",
                    DisplayText = "Sleep stage (MLP15-R2):"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 3,
                    IndicatorName = "Session_SegmentedData_Sleep_Level",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP16")),
                    MLModelFilename = "BBD_20221106__TrainingData.Sleep.MLP16__MLP16_5p00Hz-125000Hz__Session_SegmentedData_Sleep_Level__15372rows__#06_0,6588.zip",
                    DisplayText = "Sleep stage (MLP16-R2):"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 5,
                    IndicatorName = "Session_SegmentedData_BloodTest_Cholesterol",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20221028__TrainingData.BloodTest__MLP14_0p25Hz-6250Hz__Session_SegmentedData_BloodTest_Cholesterol__560rows__#00_0,9943.zip",
                    DisplayText = "Cholesterol (MLP14-R1):"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 6,
                    IndicatorName = "Session_SegmentedData_HeartRate_BeatsPerMinute",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP12")),
                    MLModelFilename = "BBD_20221122__TrainingData.HeartRate.MLP12__MLP12_0p25Hz-250Hz__Session_SegmentedData_HeartRate_BeatsPerMinute__10613rows__#42_0,5182.zip",
                    DisplayText = "Heart BPM (MLP12-R1):"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 7,
                    IndicatorName = "Session_SegmentedData_HeartRate_BeatsPerMinute",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20221122__TrainingData.HeartRate.MLP14__MLP14_0p25Hz-6250Hz__Session_SegmentedData_HeartRate_BeatsPerMinute__10613rows__#17_0,4278.zip",
                    DisplayText = "Heart BPM (MLP14-R1):"
                },
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 0,
                //    IndicatorName = "IsSubject_None",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP09")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP09_5p0Hz-150000Hz_IsSubject_None_802rows.zip",
                //    DisplayText = "Not attached?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 1,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP09")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP09_5p0Hz-150000Hz_IsSubject_AndrasFuchs_200rows.zip",
                //    DisplayText = "Andris (MLP09-200)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 2,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP09")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP09_5p0Hz-150000Hz_IsSubject_AndrasFuchs_1000rows.zip",
                //    DisplayText = "Andris (MLP09-1000)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 3,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP09")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP09_5p0Hz-150000Hz_IsSubject_AndrasFuchs_3500rows.zip",
                //    DisplayText = "Andris (MLP09-3500)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 4,
                //    IndicatorName = "IsSubject_TimeaNagy",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP09")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP09_5p0Hz-150000Hz_IsSubject_TimeaNagy_1414rows.zip",
                //    DisplayText = "Timi (MLP09-1414)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 5,
                //    IndicatorName = "IsSubject_TimeaNagy",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP09")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP09_5p0Hz-150000Hz_IsSubject_TimeaNagy_1414rows_binary.zip",
                //    DisplayText = "Timi (MLP09-1414-bin)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 6,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP10")),
                //    MLModelFilename = "BBD_20220829_TrainingData_MLP10_0p1Hz-2500Hz_IsSubject_AndrasFuchs_3500rows.zip",
                //    DisplayText = "Andris (MLP10-3500)?"
                //}
            };

            inputData.Load();

            Stopwatch sw = new();
            sw.Start();

            List<Task> tasks = new();
            foreach (IndicatorEvaluationTaskDescriptor td in taskDescriptors)
            {
                Task task = Task.Run(() =>
                {
                    IndicatorEvaluationResult evalResult = EvaluateIndicator(blockIndex, td.IndicatorIndex, td.IndicatorName, td.DisplayText, td.MLModelFilename, inputData.ApplyMLProfile(td.MLProfile));
                    if (evalResult != null)
                    {
                        lock (result)
                        {
                            result.Add(evalResult);
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            sw.Stop();
            logger?.LogTrace($"#{blockIndex:0000} Predictions were completed in {sw.ElapsedMilliseconds:N0} ms.");

            return result.OrderBy(r => r.IndicatorIndex).ToArray();
        }

        public IndicatorEvaluationResult? EvaluateIndicator(long blockIndex, int indicatorIndex, string indicatorName, string displayText, string mlModelFilename, FftDataV3 inputData)
        {
            if (!System.IO.File.Exists(mlModelFilename))
            {
                mlModelFilename = Path.Combine(GetConfig().DataDirectory, Path.GetFileName(mlModelFilename));
            }

            if (!System.IO.File.Exists(mlModelFilename))
            {
                return null;
            }

            //Create MLContext
            MLContext mlContext = new();

            // Load Trained Model
            ITransformer transformer = mlContext.Model.Load(mlModelFilename, out _);

            //var predictionEngine = mlContext.Model.CreatePredictionEngine<MLP09, MLP09Output>(transformer);

            // Input Data
            Stopwatch sw = Stopwatch.StartNew();
            if (string.IsNullOrEmpty(inputData.MLProfileName))
            {
                throw new Exception($"The input data for the ML models must be profiled first.");
            }


            IDataView? data = inputData.MLProfileName.StartsWith("MLP09")
                ? mlContext.Data.LoadFromEnumerable(new MLP09[1] { new MLP09() { Features = inputData.MagnitudeData } })
                : inputData.MLProfileName.StartsWith("MLP10")
                    ? mlContext.Data.LoadFromEnumerable(new MLP10[1] { new MLP10() { Features = inputData.MagnitudeData } })
                    : inputData.MLProfileName.StartsWith("MLP12")
                                    ? mlContext.Data.LoadFromEnumerable(new MLP12[1] { new MLP12() { Features = inputData.MagnitudeData } })
                                    : inputData.MLProfileName.StartsWith("MLP14")
                                                    ? mlContext.Data.LoadFromEnumerable(new MLP14[1] { new MLP14() { Features = inputData.MagnitudeData } })
                                                    : inputData.MLProfileName.StartsWith("MLP15")
                                                                    ? mlContext.Data.LoadFromEnumerable(new MLP15[1] { new MLP15() { Features = inputData.MagnitudeData } })
                                                                    : inputData.MLProfileName.StartsWith("MLP16")
                                                                                    ? mlContext.Data.LoadFromEnumerable(new MLP16[1] { new MLP16() { Features = inputData.MagnitudeData } })
                                                                                    : throw new Exception($"ML Profile '{inputData.MLProfileName}' is not supported yet.");
            sw.Stop();
            _logger.LogTrace($"#{blockIndex:0000} Input data setting was completed in {sw.ElapsedMilliseconds:N0} ms.");

            // Get Prediction
            sw.Restart();
            IDataView eval = transformer.Transform(data);
            IEnumerable<float> scoreColumn = eval.GetColumn<float>("Score");
            _ = scoreColumn.GetEnumerator().Current;
            RegressionMetrics metric = mlContext.Regression.Evaluate(eval, "Label");

            //var modelOutput = predictionEngine.Predict(new MLP09() { Features = inputData.MagnitudeData });
            sw.Stop();
            _logger.LogTrace($"#{blockIndex:0000} Prediction was completed in {sw.ElapsedMilliseconds:N0} ms.");

            return new IndicatorEvaluationResult()
            {
                BlockIndex = blockIndex,
                IndicatorIndex = indicatorIndex,
                IndicatorName = indicatorName,
                DisplayText = displayText,
                Value = (float)metric.MeanAbsoluteError,
                PredictionScore = (float)metric.MeanAbsoluteError
            };
        }

        public string GeneratePathToFile(DateTime captureTime)
        {
            if (!Directory.Exists(AppendDataDir(captureTime.ToString("yyyy-MM-dd"))))
            {
                _ = Directory.CreateDirectory(AppendDataDir(captureTime.ToString("yyyy-MM-dd")));
            }

            string foldername = $"{captureTime:yyyy-MM-dd}";
            string filename = $"AD2_{captureTime:yyyyMMdd_HHmmss}";
            return Path.Combine(foldername, filename);
        }

        public string GenerateBinaryFftFilename(FftDataV3 fftData, int? frameCounter, string frameCounterFormatterString)
        {
            string fftRangeString = $"__{fftData.GetFFTRange()}";
            string mlProfileString = $"__{fftData.MLProfileName.Split("_")[0]}";
            string profileOrRangeString = string.IsNullOrEmpty(mlProfileString) ? fftRangeString : mlProfileString;
            string frameCounterString = frameCounter.HasValue ? $"__fr{frameCounter.Value.ToString(frameCounterFormatterString)}" : "";
            return $"AD2_{fftData.Start.Value:yyyyMMdd_HHmmss}{profileOrRangeString}{frameCounterString}.bfft";
        }

        [Obsolete]
        public void GenerateVideo(string foldername, MLProfile mlProfile, double framerate)
        {
            int totalFrameCount = EnumerateFFTDataInFolder(foldername).Count();
            int frameCounter = 0;

            Stopwatch sw = new();
            sw.Start();
            foreach (FftDataV3 fftData in EnumerateFFTDataInFolder(foldername))
            {
                try
                {
                    frameCounter++;
                    string filenameComplete = Path.Combine(foldername, $"{fftData.Name}_{SimplifyNumber(mlProfile.MaxFrequency)}Hz_{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY.Min)}-{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY.Max)}{_config.Postprocessing.SaveAsPNG.RangeY.Unit}.png");

                    if (File.Exists(AppendDataDir(filenameComplete)))
                    {
                        //_logger.LogWarning($"{filenameComplete} already exists.");
                        continue;
                    }

                    IndicatorEvaluationResult[] evaluationResults = EvaluateIndicators(null, 0, fftData);
                    string evaluationResultsString = string.Join(System.Environment.NewLine, evaluationResults.Select(er => er.DisplayText + " " + er.PredictionScore.ToString("+0.00;-0.00; 0.00")));

                    fftData.ApplyMedianFilter();
                    fftData.ApplyCompressorFilter(0.25);

                    SaveSignalAsPng(AppendDataDir(filenameComplete), fftData, _config.Postprocessing.SaveAsPNG, mlProfile, evaluationResultsString);
                    //_logger.LogInformation($"{filenameComplete} was generated successfully.");

                    if (frameCounter % 50 == 0)
                    {
                        float percentCompleted = (float)frameCounter / totalFrameCount * 100;
                        float totalTimeToFinish = sw.ElapsedMilliseconds / (percentCompleted / 100);
                        _logger.LogInformation($"Generated {percentCompleted:0.00}% of the PNG files, {(totalTimeToFinish - sw.ElapsedMilliseconds) / 1000 / 60:0.0} minutes remaining.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"There was an error while generating PNG for '{fftData.Name}': {ex.Message}");
                }
            }
            sw.Stop();

            string mp4FilenameBase = foldername.Replace("\\", "_").Replace("#", "_");
            string mp4Filename = $"BBD_{mp4FilenameBase}_{SimplifyNumber(mlProfile.MaxFrequency)}Hz_{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY.Min)}-{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY.Max)}{_config.Postprocessing.SaveAsPNG.RangeY.Unit}.mp4";
            _logger.LogInformation($"Generating MP4 video file '{mp4Filename}'");
            try
            {
                FFmpeg.Conversions.New()
                    .SetInputFrameRate(framerate)
                    .BuildVideoFromImages(Directory.GetFiles(AppendDataDir(foldername), "*.png").OrderBy(fn => fn))
                    .SetFrameRate(framerate)
                    .SetPixelFormat(PixelFormat.yuv420p)
                    .SetOutput(AppendDataDir(mp4Filename))
                    .Start()
                    .Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an error while generating MP4 video file '{mp4Filename}': {ex.Message}");
            }
        }

        [Obsolete]
        public void GenerateMLCSV(string foldername, MLProfile mlProfile, bool includeHeaders, string tagFilterExpression, string validLabelExpression, string balanceOnTag, int? maxRows)
        {
            HashSet<string> validTags = new();
            string[] requiredTags = tagFilterExpression.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string[] validLabels = validLabelExpression.Split("+", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            List<FftDataV3> fftFilesToConvert = new();
            List<FftDataV3> fftFilesTemp;

            if (mlProfile == null)
            {
                throw new Exception("ML Profile is not specified.");
            }

            foldername = AppendDataDir(foldername);
            if (!foldername.EndsWith(Path.DirectorySeparatorChar))
            {
                foldername += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(foldername))
            {
                _logger.LogError($"The folder '{foldername}' doesn't exists.");
                return;
            }

            // Read all data files from the folder and its subfolders
            int fftFilesToConvertCount = 0;
            foreach (FftDataV3 fftData in EnumerateFFTDataInFolder(foldername))
            {
                fftFilesToConvert.Add(fftData);
                fftFilesToConvertCount++;
            }
            fftFilesToConvert = fftFilesToConvert.OrderBy(d => d.Filename).ToList();
            _logger.LogInformation($"{fftFilesToConvert.Count} valid FFT data files were found in the '{foldername}' folder.");

            // Filter data based on the tag filter
            fftFilesTemp = new List<FftDataV3>();
            if (requiredTags.Length > 0)
            {
                foreach (FftDataV3 fftData in fftFilesToConvert)
                {
                    if (requiredTags.All(rt => fftData.Tags.Any(t => t.Contains(rt))))
                    {
                        fftFilesTemp.Add(fftData);
                    }
                }
                fftFilesToConvert = fftFilesTemp;
                _logger.LogInformation($"The number of FFT data files to convert was reduced to {fftFilesToConvert.Count} by the tag filter expression '{tagFilterExpression}'.");
            }

            // Balance the data if requested
            // TODO: data balancing should be done with the final, downsampled FFT file list
            if (!string.IsNullOrWhiteSpace(balanceOnTag))
            {
                int hasBalanceTagCount = fftFilesToConvert.Count(d => d.Tags.Contains(balanceOnTag));
                int hasNoBalanceTagCount = fftFilesToConvert.Count(d => !d.Tags.Contains(balanceOnTag));

                int smallerCount = Math.Min(hasBalanceTagCount, hasNoBalanceTagCount);

                if (maxRows.HasValue && (maxRows.Value > 0))
                {
                    maxRows = (((maxRows.Value - 1) / 2) + 1) * 2;
                    smallerCount = Math.Min(smallerCount, maxRows.Value / 2);
                }

                if (smallerCount > 0)
                {
                    fftFilesTemp = new List<FftDataV3>();

                    Random rnd = new();
                    fftFilesToConvert = fftFilesToConvert.OrderBy(a => rnd.Next()).ToList();

                    hasBalanceTagCount = 0;
                    hasNoBalanceTagCount = 0;
                    foreach (FftDataV3 fftData in fftFilesToConvert)
                    {
                        bool hasBalanceTag = fftData.Tags.Contains(balanceOnTag);

                        if ((hasBalanceTag && (hasBalanceTagCount < smallerCount)) || (!hasBalanceTag && (hasNoBalanceTagCount < smallerCount)))
                        {
                            fftFilesTemp.Add(fftData);

                            if (hasBalanceTag)
                            {
                                hasBalanceTagCount++;
                            }
                            else
                            {
                                hasNoBalanceTagCount++;
                            }
                        }
                    }
                    fftFilesToConvert = fftFilesTemp.OrderBy(d => d.Filename).ToList();
                    _logger.LogInformation($"The number of valid FFT data files was reduced to {fftFilesToConvert.Count} because of the data balancing.");
                }
            }

            List<FftDataV3> convertedFFTDataCache = ConvertFFTFilesToMLProfile(mlProfile, validTags, fftFilesToConvert);


            // We have all the FFT data in downsampledFFTDataCache converted to the requested ML profile

            Dictionary<string, List<float>> labelValues = SaveMLCSVFile(foldername, mlProfile, includeHeaders, validTags, ref validLabels, convertedFFTDataCache, out string[] featureColumnNames, out string[] labelColumnNames, out string filenameRoot, out string csvFilename);

            //SaveMBConfigFile(featureColumnNames, labelColumnNames, filenameRoot, csvFilename);

            GenerateLabelDistribution(labelValues);
        }

        [Obsolete]
        private List<FftDataV3> ConvertFFTFilesToMLProfile(MLProfile mlProfile, HashSet<string> validTags, List<FftDataV3> fftFilesToConvert)
        {
            Stopwatch sw = new();
            sw.Start();

            List<FftDataV3> downsampledFFTDataCache = new();

            int fftFilesToConvertCount = fftFilesToConvert.Count();
            int fftDataLoadCompleteCount = 0;
            long fftDataBytesLoaded = 0;

            _ = Task.Run(() =>
            {
                while (fftDataLoadCompleteCount < fftFilesToConvertCount)
                {
                    if (fftDataBytesLoaded > 0)
                    {
                        _logger.LogInformation($"{(float)fftDataLoadCompleteCount / fftFilesToConvertCount * 100.0f:0.0}% of FFT data was converted with the effective speed of {(float)fftDataBytesLoaded / (1024 * 1024) / ((float)sw.ElapsedMilliseconds / 1000):0.0} MB/s.");
                    }
                    Thread.Sleep(1000);
                }
            });

            ParallelLoopResult plr = Parallel.ForEach(fftFilesToConvert, new ParallelOptions { MaxDegreeOfParallelism = 3 },
                fftData =>
                {
                    // Load the raw .bfft file into the memory
                    fftData.Load(mlProfile.Name);
                    fftDataLoadCompleteCount++;
                    fftDataBytesLoaded += fftData.FileSize;

                    if (fftData.Tags != null)
                    {
                        foreach (string tag in fftData.Tags)
                        {
                            lock (validTags)
                            {
                                _ = validTags.Add(tag);
                            }
                        }
                    }

                    if (fftData.MLProfileName != mlProfile.Name)
                    {
                        try
                        {
                            FftDataV3 downsampledFFTData = fftData.ApplyMLProfile(mlProfile);

                            lock (fftFilesToConvert)
                            {
                                FftDataV3.SaveAsBinary(downsampledFFTData, downsampledFFTData.Filename, false, mlProfile.Name);
                            }

                            lock (downsampledFFTDataCache)
                            {
                                downsampledFFTDataCache.Add(downsampledFFTData);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, $"Couldn't apply ML Profile to the file {fftData.Filename}.");
                        }

                        // Release the loaded data
                        fftData.ClearData();
                    }
                    else
                    {
                        lock (downsampledFFTDataCache)
                        {
                            downsampledFFTDataCache.Add(fftData);
                        }
                    }
                });

            while (!plr.IsCompleted)
            {
                Thread.Sleep(100);
            }
            fftFilesToConvert.Clear();

            fftDataLoadCompleteCount = fftFilesToConvertCount;

            sw.Stop();
            _logger.LogInformation($"100.0% of FFT data was loaded in {sw.ElapsedMilliseconds:N0} ms with the effective speed of {(float)fftDataBytesLoaded / (1024 * 1024) / ((float)sw.ElapsedMilliseconds / 1000):0.0} MB/s.");

            return downsampledFFTDataCache;
        }

        private Dictionary<string, List<float>> SaveMLCSVFile(string foldername, MLProfile mlProfile, bool includeHeaders, HashSet<string> validTags, ref string[] validLabels, List<FftDataV3> fftDataCache, out string[] featureColumnNames, out string[] labelColumnNames, out string filenameRoot, out string csvFilename)
        {
            StringBuilder sb = new();
            int featureColumnIndexStart = 0;
            int featureColumnIndexEnd = 0;
            DateTimeOffset nextConsoleFeedback = DateTime.UtcNow;
            int fftDataCount = fftDataCache.Count();
            int dataRowsWritten = 0;

            if (fftDataCount == 0)
            {
                throw new Exception($"There are no valid FFT files in the '{foldername}' folder.");
            }

            Dictionary<string, List<float>> labelValues = new();

            FftDataV3? templateFftData = fftDataCache[0];
            featureColumnIndexStart = 0;
            featureColumnIndexEnd = fftDataCache[0].MagnitudeData.Length;

            if (validLabels.Length == 0)
            {
                validLabels = validTags.ToArray();
            }

            featureColumnNames = Enumerable.Range(featureColumnIndexStart, featureColumnIndexEnd - featureColumnIndexStart).Select(i => "Freq_" + templateFftData.GetBinFromIndex(i).ToString("0.00").Replace(".", "p") + "_Hz").ToArray();
            labelColumnNames = validLabels.Select(l => (validTags.Contains(l) ? "Is" + l : l).Replace(".", "_")).ToArray();
            if (includeHeaders)
            {
                string header = string.Join(",", featureColumnNames);
                header += "," + string.Join(",", labelColumnNames);

                _ = sb.AppendLine(header);
            }

            filenameRoot = $"BBD_{fftDataCache.Max(d => d.FileModificationTimeUtc.Value):yyyyMMdd}__{Path.GetFileName(Path.GetDirectoryName(foldername))}__{mlProfile.Name}" + (labelColumnNames.Length == 1 ? $"__{labelColumnNames[0]}" : "") + $"__{fftDataCount}rows";
            csvFilename = filenameRoot + ".csv";
            _logger.LogInformation($"Generating machine learning CSV file '{csvFilename}' with {featureColumnIndexEnd - featureColumnIndexStart + validLabels.Length} columns and {fftDataCount + 1} rows.");
            try
            {
                File.WriteAllText(AppendDataDir(csvFilename), sb.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an error while generating headers for CSV file '{csvFilename}': {ex.Message}");
            }
            _ = sb.Clear();

            int i = 0;
            int missingLabelValue = 0;
            foreach (FftDataV3? fftData in fftDataCache.OrderBy(d => d.Start))
            {
                if (fftData == null)
                {
                    continue;
                }

                // load a new location if needed
                string? locationAlias = null;
                string? locationTag = fftData.Tags?.FirstOrDefault(t => t.StartsWith("Location_"));
                if (locationTag != null)
                {
                    locationAlias = locationTag["Location_".Length..];
                }

                // load a new subject if needed
                string? subjectAlias = null;
                string? subjectTag = fftData.Tags?.FirstOrDefault(t => t.StartsWith("Subject_"));
                if (subjectTag != null)
                {
                    subjectAlias = subjectTag["Subject_".Length..];
                }

                Sessions.Session session = _sessionManager.StartSession(locationAlias, subjectAlias);


                if (nextConsoleFeedback < DateTime.UtcNow)
                {
                    _logger.LogInformation($"Adding '{fftData.Name}' to the machine learning CSV file. {(float)i / fftDataCount * 100.0f:0.0}% done.");
                    nextConsoleFeedback = DateTime.UtcNow.AddSeconds(3);

                    try
                    {
                        File.AppendAllText(AppendDataDir(csvFilename), sb.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"There was an error while appending to CSV file '{csvFilename}': {ex.Message}");
                    }
                    _ = sb.Clear();
                }

                bool skipDataset = false;
                string datasetString = string.Join(",", fftData.MagnitudeData[featureColumnIndexStart..featureColumnIndexEnd].Select(md => md.ToString("0.0000000000", System.Globalization.CultureInfo.InvariantCulture.NumberFormat)));
                foreach (string validLabel in validLabels)
                {
                    // there are two separate cases here based on where we need to get the value of the label from                        
                    float labelValue = 0;
                    if (validTags.Contains(validLabel))
                    {
                        // A, if the validLabel is a tag then we simply need to set it to 0 or 1
                        labelValue = fftData.Tags.Contains(validLabel) ? 1.00f : 0.00f;
                    }
                    else
                    {
                        // B, the validLabel isn't a known tag so we suppose that it is a property in metadata
                        if (fftData.Start.HasValue && fftData.End.HasValue)
                        {
                            //DateTimeOffset timeUtc = new DateTimeOffset((fftData.Start.Value.Ticks + fftData.End.Value.Ticks) / 2, TimeSpan.Zero);

                            if (!session.TryToGetValue(validLabel, fftData.Start.Value.ToUniversalTime(), fftData.End.Value.ToUniversalTime(), out labelValue))
                            {
                                // we don't have a valid metadata value so we should skip this entry
                                skipDataset = true;
                                break;
                            }
                            else
                            {
                                missingLabelValue++;
                                //_logger.LogInformation($"The value of '{validLabel}' at '{timeUtc}' (UTC) is '{labelValue}'.");
                            }
                        }
                    }

                    if (!labelValues.ContainsKey(validLabel))
                    {
                        labelValues.Add(validLabel, new List<float>());
                    }

                    labelValues[validLabel].Add(labelValue);

                    datasetString += "," + labelValue.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                }

                if (!skipDataset)
                {
                    _ = sb.AppendLine(datasetString);
                    dataRowsWritten++;
                }
                i++;
            }

            if (missingLabelValue > 0)
            {
                _logger.LogWarning($"There were {missingLabelValue} data entries that didn't have label values. Those were not included in the CSV file.");
            }

            try
            {
                File.AppendAllText(AppendDataDir(csvFilename), sb.ToString());

                string newCsvFilename = $"BBD_{fftDataCache.Max(d => d.FileModificationTimeUtc.Value):yyyyMMdd}__{Path.GetFileName(Path.GetDirectoryName(foldername))}__{mlProfile.Name}" + (labelColumnNames.Length == 1 ? $"__{labelColumnNames[0]}" : "") + $"__{dataRowsWritten}rows.csv";
                File.Move(AppendDataDir(csvFilename), AppendDataDir(newCsvFilename));
                csvFilename = newCsvFilename;
            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an error while appending to CSV file '{csvFilename}': {ex.Message}");
            }
            _ = sb.Clear();

            return labelValues;
        }

        private void SaveMBConfigFile(string[] featureColumnNames, string[] labelColumnNames, string filenameRoot, string csvFilename)
        {
            string mbconfigFilename = filenameRoot + ".mbconfig";
            _logger.LogInformation($"Generating mbconfig file '{mbconfigFilename}'.");
            try
            {
                MBConfig mbConfig = new(AppendDataDir(csvFilename), featureColumnNames, labelColumnNames);
                string mbConfigJson = JsonSerializer.Serialize(mbConfig, new JsonSerializerOptions() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                File.WriteAllText(AppendDataDir(mbconfigFilename), mbConfigJson);
            }
            catch (Exception ex)
            {
                _logger.LogError($"There was an error while generating mbconfig file '{mbconfigFilename}': {ex.Message}");
            }
        }

        private void GenerateLabelDistribution(Dictionary<string, List<float>> labelValues)
        {
            if (labelValues.Count > 0)
            {
                foreach (string labelName in labelValues.Keys)
                {
                    _logger.LogInformation($"Label statistics for {labelName}");

                    int distinctValueCount = labelValues[labelName].Distinct().Count();
                    int bucketCount = 10;
                    float bucketWidth = (labelValues[labelName].Max() - labelValues[labelName].Min()) / bucketCount;

                    _logger.LogInformation($" min = {labelValues[labelName].Min():0.0000} | avg = {labelValues[labelName].Average():0.0000} | max = {labelValues[labelName].Max():0.0000} | count = {labelValues[labelName].Count()} | non-zero = {labelValues[labelName].Count(v => v > 0)} | distinct = {distinctValueCount}");
                    _logger.LogInformation($" Distribution");

                    for (int j = 0; j < bucketCount; j++)
                    {
                        float bucketMin = labelValues[labelName].Min() + (j * bucketWidth);
                        float bucketMax = bucketMin + bucketWidth;
                        int bucketItemCount = labelValues[labelName].Count(v => (v > bucketMin) && (v <= bucketMax));
                        if (j == 0)
                        {
                            bucketItemCount += labelValues[labelName].Count(v => v == bucketMin);
                        }
                        _logger.LogInformation($"  {bucketMin:0.0000} - {bucketMax:0.0000}: {bucketItemCount,7}");
                    }
                }
            }
        }

        [Obsolete]
        public void TestAllModels(string foldername, float percentageToTest)
        {
            _logger.LogInformation($"Evaluating models based on {percentageToTest}% of the data in the '{foldername}' folder.");

            Random rnd = new();
            Dictionary<string, int[]> confusionMatrix = new();
            foldername = AppendDataDir(foldername);

            if (Directory.Exists(foldername))
            {
                int i = 0;
                foreach (FftDataV3 fftData in EnumerateFFTDataInFolder(foldername))
                {
                    if (rnd.NextDouble() > percentageToTest / 100.0)
                    {
                        continue;
                    }

                    _logger.LogInformation($"Evaluating {fftData.Name}.");
                    IndicatorEvaluationResult[] evaluationResults = EvaluateIndicators(_logger, i++, fftData);

                    foreach (IndicatorEvaluationResult er in evaluationResults)
                    {
                        if (!confusionMatrix.ContainsKey(er.IndicatorName))
                        {
                            confusionMatrix.Add(er.IndicatorName, new int[4]);
                        }

                        if (er.IsTruePositive)
                        {
                            confusionMatrix[er.IndicatorName][0]++;
                        }

                        if (er.IsFalseNegative)
                        {
                            confusionMatrix[er.IndicatorName][1]++;
                        }

                        if (er.IsFalsePositive)
                        {
                            confusionMatrix[er.IndicatorName][2]++;
                        }

                        if (er.IsTrueNegative)
                        {
                            confusionMatrix[er.IndicatorName][3]++;
                        }
                    }
                }
            }

            _logger.LogInformation($"");
            foreach (KeyValuePair<string, int[]> cm in confusionMatrix)
            {
                _logger.LogInformation($"Confusion matrix for the '{cm.Key}' indicator:");
                _logger.LogInformation($"    +--------------------------------+");
                _logger.LogInformation($"    |     Prediction condition       |");
                _logger.LogInformation($"    |----------+----------+----------|");
                _logger.LogInformation($"    |  Total   |          |          |");
                _logger.LogInformation($"    | {string.Format("{0,7}", cm.Value[0] + cm.Value[1] + cm.Value[2] + cm.Value[3])}  | positive | negative |");
                _logger.LogInformation($"+---|----------|----------|----------|");
                _logger.LogInformation($"| a | positive |TP:{string.Format("{0,7}", cm.Value[0])}|FN:{string.Format("{0,7}", cm.Value[1])}|");
                _logger.LogInformation($"| c |----------|----------|----------|");
                _logger.LogInformation($"| t | negative |FP:{string.Format("{0,7}", cm.Value[2])}|TN:{string.Format("{0,7}", cm.Value[3])}|");
                _logger.LogInformation($"+---+----------+----------+----------+");
                _logger.LogInformation($"Accuracy: {(cm.Value[0] + cm.Value[3]) / (double)(cm.Value[0] + cm.Value[1] + cm.Value[2] + cm.Value[3]) * 100:##0.00}%");
                _logger.LogInformation($"");
            }

        }
        public IEnumerable<FftDataV3> EnumerateFFTDataInFolder(string foldername, string[]? applyTags = null)
        {
            applyTags ??= Array.Empty<string>();

            if (Path.GetFileName(foldername).StartsWith("."))
            {
                yield break;
            }

            if (Directory.Exists(AppendDataDir(foldername)))
            {
                foreach (string filename in Directory.GetFiles(AppendDataDir(foldername)).OrderBy(n => n))
                {
                    string pathToFile = Path.GetFullPath(filename);

                    if (Path.GetExtension(filename) is not ".bfft" and not ".fft" and not ".zip")
                    {
                        continue;
                    }

                    FftDataV3? fftData = null;
                    try
                    {
                        FileInfo fi = new(pathToFile);

                        fftData = new FftDataV3
                        {
                            Filename = pathToFile,
                            FileSize = fi.Length,
                            FileModificationTimeUtc = fi.LastWriteTimeUtc
                        };

                        if (string.IsNullOrEmpty(fftData.Name))
                        {
                            fftData.Name = Path.GetFileNameWithoutExtension(pathToFile);
                        }

                        if (applyTags != null)
                        {
                            List<string> allTags = new(fftData.Tags);
                            allTags.AddRange(applyTags);
                            fftData.Tags = allTags.ToArray();
                        }
                        else
                        {
                            fftData.Tags ??= new string[0];
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    if (fftData != null)
                    {
                        yield return fftData;
                    }
                }

                foreach (string directoryName in Directory.GetDirectories(AppendDataDir(foldername)).OrderBy(n => n))
                {
                    List<string> tags = new(applyTags);
                    string newTag = Path.GetFileName(directoryName);
                    if (newTag.StartsWith("#"))
                    {
                        tags.Add(newTag[1..]);
                    }

                    foreach (FftDataV3 fftData in EnumerateFFTDataInFolder(directoryName, tags.ToArray()))
                    {
                        yield return fftData;
                    }
                }
            }
        }

        public string AppendDataDir(string filename)
        {
            return !string.IsNullOrWhiteSpace(_config.DataDirectory)
                ? Path.Combine(_config.DataDirectory, filename)
                : Path.Combine(Directory.GetCurrentDirectory(), filename);
        }

        [Obsolete]
        public void SaveSignalAsPng(string filename, FftDataV3 fftData, SaveAsPngOptions config, MLProfile mlProfile, string? notes = null)
        {
            Size targetResolution = new(config.TargetWidth, config.TargetHeight);
            _ = targetResolution.Width / (float)targetResolution.Height;

            fftData.Load();
            fftData = fftData.ApplyMLProfile(mlProfile);
            string tags = string.Join(", ", fftData.Tags ?? Array.Empty<string>());

            float maxValue = fftData.MagnitudeData.Max();
            int maxValueAtIndex = Array.IndexOf(fftData.MagnitudeData, maxValue);

            int sampleCount;

            int samplesPerRow;
            do
            {
                sampleCount = fftData.MagnitudeData.Length;
                samplesPerRow = 1;
                while (samplesPerRow < sampleCount && fftData.GetBinFromIndex(samplesPerRow).EndFrequency <= config.RangeX)
                {
                    samplesPerRow++;
                }

                if (samplesPerRow > 9500)
                {
                    fftData = fftData.Downsample(fftData.FrequencyStep * 2);
                }
            } while (samplesPerRow > 9500);

            float[] samples = fftData.MagnitudeData;
            int rowCount = (int)Math.Ceiling((float)sampleCount / samplesPerRow);

            float resoltionScaleDownFactor = (float)samplesPerRow / targetResolution.Width;

            int rowHeight = (int)(resoltionScaleDownFactor * targetResolution.Height) / rowCount;

            Color bgColor = Color.FromArgb(0x23, 0x12, 0x18);

            if (samplesPerRow > 9500)
            {
                throw new Exception($"We supposed to generate a bitmap with {samplesPerRow} pixel width. Width must be less than 9500 pixels.");
            }

            if (rowCount * rowHeight > 55000)
            {
                throw new Exception($"We supposed to generate a bitmap with {rowCount * rowHeight} pixel height. Height must be less than 55000 pixels.");
            }

            Bitmap spectrumBitmap = new(samplesPerRow, rowHeight * rowCount, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Graphics graphics = Graphics.FromImage(spectrumBitmap);
            graphics.FillRectangle(new SolidBrush(bgColor), new Rectangle(0, 0, samplesPerRow, rowHeight * rowCount));

            if (chartPens == null)
            {
                chartPens = new Pen[6];
                chartPens[0] = new Pen(new SolidBrush(Color.FromArgb(0x8D, 0x49, 0x60)), 1.0f);
                chartPens[1] = new Pen(new SolidBrush(Color.FromArgb(0x96, 0x6C, 0x95)), 1.0f);
                chartPens[2] = new Pen(new SolidBrush(Color.FromArgb(0x94, 0x92, 0xBF)), 1.0f);
                chartPens[3] = new Pen(new SolidBrush(Color.FromArgb(0x94, 0xB7, 0xDC)), 1.0f);
                chartPens[4] = new Pen(new SolidBrush(Color.FromArgb(0xA5, 0xDB, 0xED)), 1.0f);
                chartPens[5] = new Pen(new SolidBrush(Color.FromArgb(0xC9, 0xFB, 0xFA)), 1.0f);
            }

            float sampleAmplification = 1.0f / (config.RangeY.Max - config.RangeY.Min) * rowHeight;

            for (int r = 1; r <= rowCount; r++)
            {
                int bottomLine = r * rowHeight;

                for (int i = 0; i < samplesPerRow; i++)
                {
                    int dataPointIndex = ((r - 1) * samplesPerRow) + i;
                    if (dataPointIndex >= samples.Length)
                    {
                        continue;
                    }

                    if (samples[dataPointIndex] > 0)
                    {
                        int valueToShow = (int)Math.Min((samples[dataPointIndex] - config.RangeY.Min) * sampleAmplification, rowHeight);
                        if (valueToShow < 0)
                        {
                            valueToShow = 0;
                        }

                        int penIndex = (int)Math.Min(chartPens.Length - 1, (float)valueToShow / rowHeight * 5);

                        graphics.DrawLine(chartPens[penIndex], new Point(i, bottomLine), new Point(i, bottomLine - valueToShow));
                    }
                }
            }

            int scaledDownWidth = (int)(spectrumBitmap.Width / resoltionScaleDownFactor) / 2 * 2;
            int scaledDownHeight = (int)(spectrumBitmap.Height / resoltionScaleDownFactor) / 2 * 2;
            Bitmap scaledDownBitmap = new(spectrumBitmap, new Size(scaledDownWidth, scaledDownHeight));

            Font font = new("Georgia", 10.0f);
            graphics = Graphics.FromImage(scaledDownBitmap);
            graphics.DrawString($"{Path.GetFileName(filename)}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + (font.Height * 0)));
            graphics.DrawString($"{tags}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + (font.Height * 1.1f)));
            graphics.DrawString($"Y range: {config.RangeY.Min.ToString(config.RangeY.Format)}-{config.RangeY.Max.ToString(config.RangeY.Format)} {config.RangeY.Unit}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + (font.Height * 2.2f)));
            graphics.DrawString($"Max value: {maxValue.ToString(config.RangeY.Format)} {config.RangeY.Unit} @ {fftData.GetBinFromIndex(maxValueAtIndex)}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + (font.Height * 3.3f)));

            if (!string.IsNullOrEmpty(notes))
            {
                int noteIndex = 0;
                foreach (string note in notes.Split(System.Environment.NewLine))
                {
                    noteIndex++;
                    graphics.DrawString(note, font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + (font.Height * (5.5f + (noteIndex * 1.1f)))));
                }
            }

            int scaledDownRowHeight = scaledDownHeight / rowCount;
            for (int r = 1; r <= rowCount; r++)
            {
                FftBin fftBinStart = fftData.GetBinFromIndex((r - 1) * samplesPerRow);
                FftBin fftBinEnd = fftData.GetBinFromIndex((r * samplesPerRow) - 1);
                string freqStart = SimplifyNumber(fftBinStart.StartFrequency) + "Hz";
                string freqEnd = SimplifyNumber(fftBinEnd.EndFrequency) + "Hz";
                int bottomLine = r * scaledDownRowHeight;

                // string on the left side of the row
                graphics.DrawString($"{freqStart}", font, Brushes.White, new PointF(10.0f, bottomLine - (scaledDownRowHeight * 0.25f)));

                // string on the right side of the row
                graphics.DrawString($"{freqEnd}", font, Brushes.White, new PointF(scaledDownWidth - 75.0f, bottomLine - (scaledDownRowHeight * 0.25f)));
            }

            FileStream pngFile = new(filename, FileMode.Create);
            scaledDownBitmap.Save(pngFile, System.Drawing.Imaging.ImageFormat.Png);
            pngFile.Close();
        }

        public string SimplifyNumber(double n, string format = "0.###")
        {
            string[] postfixes = { "p", "n", "u", "m", "", "k", "M", "T", "P" };
            int postfixIndex = 4;

            if (n == 0)
            {
                return n.ToString(format);
            }

            double absValue = Math.Abs(n);

            while (absValue > 1 && absValue % 1000 == 0)
            {
                postfixIndex++;
                absValue /= 1000;
            }

            while (absValue < 1)
            {
                postfixIndex--;
                absValue *= 1000;
            }

            return absValue.ToString(format) + postfixes[postfixIndex];
        }

        internal static void ShowWelcomeScreen(string versionString)
        {
            Console.WriteLine($"Bio Balance Detector Body Monitor v{versionString}");
            Console.WriteLine();
            Console.WriteLine("Command line options:");
            Console.WriteLine(" --calibration");
            Console.WriteLine("         Calibrate the hardware by connecting the W2 signal generator to the CH1 input.");
            Console.WriteLine();
            Console.WriteLine(" --mlcsv <FFT data directory> <filter> <machine learning profile name> <tag requirement expression> <feature column name>");
            Console.WriteLine("         Generate CSV file based on the profile for machine learning from FFT data that have the tag(s), and create the .mbconfig file for the trainer.");
            Console.WriteLine("         e.g. --mlcsv TrainingData \"MLP05\" \"Subject_\" \"Subject_Andras\"");
            Console.WriteLine();
            Console.WriteLine(" --testmodels <FFT data directory> <percent to test>");
            Console.WriteLine("         Test the existing models with a given percentage of the data and generate their confusion matrix.");
            Console.WriteLine("         e.g. --testmodels TrainingData 5%");
            Console.WriteLine();
            Console.WriteLine(" --video <FFT data directory> <machine learning profile> <target resolution>");
            Console.WriteLine("         Generate PNG images and an MP4 video from FFT data using ffmpeg.");
            Console.WriteLine("         e.g. --video 2022-05-31 \"MLP04\" 1920x1080");
            Console.WriteLine();
            Console.WriteLine("Check appsettings.json for more options.");
            Console.WriteLine();
        }

        [Obsolete]
        internal void ProcessCommandLineArguments(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "--video")
                {
                    MLProfile? mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => !string.IsNullOrWhiteSpace(_config.Postprocessing.SaveAsPNG.MLProfile) && p.Name.StartsWith(_config.Postprocessing.SaveAsPNG.MLProfile));

                    if (args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]))
                    {
                        mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(args[2]));

                        if (mlProfile == null)
                        {
                            _logger.LogError($"The profile '{args[2]}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                            return;
                        }
                    }

                    if (args.Length > 3 && !string.IsNullOrWhiteSpace(args[3]) && args[3].Contains("x"))
                    {

                        if (int.TryParse(args[3].Split("x")[0], out int width) && int.TryParse(args[3].Split("x")[1], out int height))
                        {
                            _config.Postprocessing.SaveAsPNG.TargetWidth = width;
                            _config.Postprocessing.SaveAsPNG.TargetHeight = height;
                        }
                    }

                    GenerateVideo(args[1], mlProfile, framerate: 12);
                    return;
                }

                if (args[0] == "--testmodels")
                {
                    if (args.Length != 3 || !args[2].EndsWith("%"))
                    {
                        _logger.LogInformation($"Usage:");
                        _logger.LogInformation($"bbd.bodymonitor.exe --testmodels <traning data folder> <percentage to test>");
                        return;
                    }

                    float percentageToTest = float.Parse(args[2][..^1]);

                    PreprareMachineLearningModels();
                    TestAllModels(args[1], percentageToTest);
                    return;
                }

                if (args[0] == "--calibrate")
                {
                    string deviceSerialNumber = string.Empty;
                    if (args.Length > 1)
                    {
                        deviceSerialNumber = args[1];
                    }

                    CalibrateDevice(deviceSerialNumber);
                    return;
                }

                if (args[0] == "--dataacquisition")
                {
                    string deviceSerialNumber = string.Empty;
                    if (args.Length > 1)
                    {
                        deviceSerialNumber = args[1];
                    }

                    if (_config.Indicators.Enabled)
                    {
                        PreprareMachineLearningModels();
                    }

                    _ = StartDataAcquisition(deviceSerialNumber);
                }
            }
        }

        internal static string GetFullMLPath(string filename)
        {
            return Path.GetFullPath(@"MLModels\" + filename);
        }

        public BodyMonitorOptions GetConfig()
        {
            return _config;
        }

        public void SetConfig(BodyMonitorOptions config)
        {
            _config = config;
        }

        public void GenerateFFT(string foldername, MLProfile mlProfile, float interval)
        {
            float dataBlockLength = _config.Postprocessing.DataBlock;

            string folderFullPath = AppendDataDir(foldername);

            if (Directory.Exists(folderFullPath))
            {
                long totalBytesToProcess = 0;
                long totalBytesProcessed = 0;
                if (Directory.Exists(folderFullPath))
                {
                    foreach (string filename in Directory.GetFiles(folderFullPath, "*.wav", SearchOption.AllDirectories).OrderBy(n => n))
                    {
                        string wavFilename = Path.GetFullPath(filename);

                        FileInfo fi = new(wavFilename);
                        totalBytesToProcess += fi.Length;
                    }
                }

                Stopwatch sw = new();
                _ = Task.Run(() =>
                {
                    while (totalBytesProcessed < totalBytesToProcess)
                    {
                        if (totalBytesProcessed > 0)
                        {
                            float percentCompleted = (float)totalBytesProcessed / totalBytesToProcess * 100;
                            float totalTimeToFinish = sw.ElapsedMilliseconds / (percentCompleted / 100);
                            _logger.LogInformation($"Generated {percentCompleted:0.00}% of the FFT files, {(totalTimeToFinish - sw.ElapsedMilliseconds) / 1000 / 60:0.0} minutes remaining.");
                        }
                        Thread.Sleep(1000);
                    }
                });

                foreach (string filename in Directory.GetFiles(folderFullPath, "*.wav", SearchOption.AllDirectories).OrderBy(n => n))
                {
                    string wavFilename = Path.GetFullPath(filename);

                    try
                    {
                        FileInfo fi = new(wavFilename);
                        totalBytesProcessed += fi.Length;

                        FileStream waveFileStream = new(wavFilename, FileMode.Open);
                        WavePcmFormat waveHeader = WaveFileExtensions.ReadWaveFileHeader(waveFileStream);

                        FftDataBlockCache fftDataBlockCacheTemp = new((int)waveHeader.SampleRate, _config.Postprocessing.FFTSize, dataBlockLength, _config.Postprocessing.ResampleFFTResolutionToHz);

                        float waveFileTotalLength = (float)waveHeader.DataChuckSize / (waveHeader.SampleRate * waveHeader.BytesPerSample);
                        DateTime lastWriteTime = File.GetLastWriteTimeUtc(wavFilename);
                        DateTime estimatedStartTime = lastWriteTime.AddSeconds(-waveFileTotalLength);

                        int frameCounter = 0;
                        int totalFrameCount = (int)(waveFileTotalLength / interval);
                        string frameCounterFormatterString = new('0', totalFrameCount.ToString().Length);

                        sw.Start();
                        for (float position = 0.0f; position + dataBlockLength < waveFileTotalLength; position += interval)
                        {
                            frameCounter++;

                            DateTime currentTime = estimatedStartTime.AddSeconds(position);

                            DiscreteSignal ds = WaveFileExtensions.ReadAsDiscreateSignal(waveFileStream, position, dataBlockLength);
                            FftDataV3 fftData = fftDataBlockCacheTemp.CreateFftData(ds, currentTime, (int)(waveHeader.SampleRate * dataBlockLength));
                            fftData = fftData.ApplyMLProfile(mlProfile);
                            string pathToFile = Path.Combine(Path.GetDirectoryName(wavFilename), GenerateBinaryFftFilename(fftData, frameCounter, frameCounterFormatterString));
                            FftDataV3.SaveAsBinary(fftData, pathToFile, false);
                        }
                        sw.Stop();
                        //_logger.LogInformation($"Generated all the FFT files for '{filename}'.");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"There was an error while generating FFT files for {wavFilename}: {ex.Message}");
                    }
                }
                _logger.LogInformation($"Generated 100% of the FFT files in {sw.ElapsedMilliseconds / 1000 / 60:0.0} minutes.");
            }
        }

        /// <summary>
        /// Appends the .WAV files in the data folder and its subfolders to a single .EDF file in the given time range.
        /// </summary>
        /// <param name="dataFoldername">The root folder of the WAV files</param>
        /// <param name="fromDateTime">Time from which the WAV files are read</param>
        /// <param name="toDateTime">Time until the WAV files are read</param>
        public void GenerateEDF(string dataFoldername, DateTime fromDateTime, DateTime toDateTime)
        {
            string folderFullPath = AppendDataDir(dataFoldername);
            SortedDictionary<DateTime, string> filesToAppend = new();

            DateTime minFileCreationTime = DateTime.MaxValue;
            DateTime maxFileCreationTime = DateTime.MinValue;
            if (Directory.Exists(folderFullPath))
            {
                long totalBytesToProcess = 0;
                long totalBytesProcessed = 0;
                if (Directory.Exists(folderFullPath))
                {
                    foreach (string filename in Directory.GetFiles(folderFullPath, "*.wav", SearchOption.AllDirectories).OrderBy(n => n))
                    {
                        string wavFilename = Path.GetFullPath(filename);

                        FileInfo fi = new(wavFilename);

                        if ((fi.CreationTimeUtc >= fromDateTime) && (fi.CreationTimeUtc <= toDateTime))
                        {
                            filesToAppend.Add(fi.CreationTimeUtc, wavFilename);
                            totalBytesToProcess += fi.Length;

                            if (fi.CreationTimeUtc < minFileCreationTime)
                            {
                                minFileCreationTime = fi.CreationTimeUtc;
                            }

                            if (fi.CreationTimeUtc > maxFileCreationTime)
                            {
                                maxFileCreationTime = fi.CreationTimeUtc;
                            }
                        }
                    }
                }

                _logger.LogInformation($"Generating EDF file from {filesToAppend.Count} WAV files.");

                Stopwatch sw = new();
                _ = Task.Run(() =>
                {
                    while (totalBytesProcessed < totalBytesToProcess)
                    {
                        if (totalBytesProcessed > 0)
                        {
                            float percentCompleted = (float)totalBytesProcessed / totalBytesToProcess * 100;
                            float totalTimeToFinish = sw.ElapsedMilliseconds / (percentCompleted / 100);
                            _logger.LogInformation($"Generated {percentCompleted:0.00}% of the EDF file, {(totalTimeToFinish - sw.ElapsedMilliseconds) / 1000 / 60:0.0} minutes remaining.");
                        }
                        Thread.Sleep(1000);
                    }
                });

                sw.Start();
                List<EDFSignal> edfSignals = new();
                AnnotationSignal annotations = new(64);
                EDFSignal? signal = null;
                WaveFormat waveFormat = new();

                int numberOfSamplesPerRecord = 0;
                double recordDurationInSeconds = 0.1;

                DateTime? edfStartTime = null;

                foreach (string filename in filesToAppend.Values)
                {
                    try
                    {
                        FileInfo fi = new(filename);
                        WaveFile waveFile = new(File.ReadAllBytes(filename));
                        totalBytesProcessed += fi.Length;

                        if (waveFormat.BitsPerSample == 0)
                        {
                            // this is the first file, so we can set the start time of the EDF file
                            edfStartTime = fi.CreationTimeUtc;

                            // The first file defines the technical parameters of the EDF file
                            waveFormat = waveFile.WaveFmt;
                            signal = new EDFSignal(edfSignals.Count, waveFormat.SamplingRate);
                            // The EDF file will contain 0.1 seconds of data in each data record
                            numberOfSamplesPerRecord = (int)(waveFormat.SamplingRate * recordDurationInSeconds);
                            signal.NumberOfSamplesInDataRecord.Value = numberOfSamplesPerRecord;
                        }
                        else
                        {
                            // The other files must match the technical parameters of the first file
                            if ((waveFormat.BitsPerSample != waveFile.WaveFmt.BitsPerSample)
                                || (waveFormat.ChannelCount != waveFile.WaveFmt.ChannelCount)
                                || (waveFormat.SamplingRate != waveFile.WaveFmt.SamplingRate))
                            {
                                _logger.LogWarning($"The technical parameters of the WAV file '{filename}' doesn't match the others so it must be skipped.");
                            }
                        }

                        short[] samplesToWrite = waveFile.Signals.First().Samples.Select(s => (short)(s * 32767)).ToArray();

                        double durationSeconds = samplesToWrite.Length / waveFormat.SamplingRate;
                        annotations.Samples.Add(new TAL((fi.CreationTimeUtc - edfStartTime.Value).TotalSeconds, durationSeconds, fi.Name));
                        signal?.Samples.AddRange(samplesToWrite);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"There was an error while appending {filename}: {ex.Message}");
                    }
                }

                if (signal != null)
                {
                    signal.Label.Value = "Right forearm";
                    signal.TransducerType.Value = "TENS contact - forearm";
                    signal.PhysicalDimension.Value = "uV";
                    signal.PhysicalMinimum.Value = -1000;
                    signal.PhysicalMaximum.Value = +1000;
                    signal.DigitalMinimum.Value = -32767;
                    signal.DigitalMaximum.Value = +32767;
                    signal.Prefiltering.Value = "N/A";
                    signal.NumberOfSamplesInDataRecord.Value = numberOfSamplesPerRecord;
                    signal.Reserved.Value = "";

                    edfSignals.Add(signal);

                    int numberOfDataRecords = (int)Math.Ceiling((double)signal.Samples.Count / signal.NumberOfSamplesInDataRecord.Value);
                    short numberOfSignalsInRecord = 1;

                    string subjectCode = "MCH-0234567";
                    string subjectName = "Jane_Frank";
                    string subjectSex = "F";
                    string subjectBirthdate = "02-MAY-1951";
                    string patientId = subjectCode + " " + subjectSex + " " + subjectBirthdate + " " + subjectName;
                    patientId = patientId[..Math.Min(80, patientId.Length)];

                    string recordStartdate = "Startdate " + edfStartTime.Value.ToString("dd-MMM-yyyy").ToUpperInvariant().Replace(".", "");
                    string recordAdminCode = "EMG987";
                    string recordTechnician = "KL/HIJ";
                    string recordDevice = "Digilent_AD2";
                    string recordAdditionalInfo = "MNC_R_Median_Nerve";
                    string recordId = recordStartdate + " " + recordAdminCode + " " + recordTechnician + " " + recordDevice + " " + recordAdditionalInfo;
                    recordId = recordId[..Math.Min(80, recordId.Length)];

                    EDFHeader edfHeader = new("0", patientId, recordId, minFileCreationTime.ToString("dd.MM.yy"), minFileCreationTime.ToString("HH.mm.ss"), 768, "EDF+D", numberOfDataRecords, recordDurationInSeconds, numberOfSignalsInRecord, new string[1] { "Right forearm" }, new string[1] { "TENS contact - forearm" }, new string[1] { "uV" }, new double[1] { -1000 }, new double[1] { +1000 }, new int[1] { -32767 }, new int[1] { +32767 }, new string[1] { "N/A" }, new int[1] { numberOfSamplesPerRecord }, new string[1] { "" });

                    EDFFile edfFile = new(edfHeader, edfSignals.ToArray(), new List<AnnotationSignal>(new[] { annotations }));

                    try
                    {
                        edfFile.Save(AppendDataDir($"EDF_{minFileCreationTime:yyyyMMdd_HHmmss}__{maxFileCreationTime:yyyyMMdd_HHmmss}.edf"));
                    }
                    catch (IOException ex)
                    {
                        throw new Exception($"There was an error while saving the EDF file: {ex.Message}");
                    }

                    _logger.LogInformation($"Generated 100% of the EDF file in {((float)sw.ElapsedMilliseconds) / 1000 / 60:0.0} minutes.");
                }

                sw.Stop();
            }
        }

        private int GetDeviceIndexFromSerialNumber(string deviceSerialNumber)
        {
            ConnectedDevice? device = ListDevices().FirstOrDefault(d => d.SerialNumber.ToLowerInvariant() == deviceSerialNumber?.ToLowerInvariant());

            return device != null ? device.Index : -1;
        }
    }
}
