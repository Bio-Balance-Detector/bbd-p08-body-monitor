using Microsoft.Extensions.Logging;
using Microsoft.ML;
using NWaves.Audio;
using NWaves.Signals;
using NWaves.Transforms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace BBD.BodyMonitor.CLI
{
    internal class Helpers
    {
        private readonly ILogger _logger;
        private BodyMonitorConfig _config;

        DataAcquisition dataAcquisition;
        DataAcquisition calibration;
        FftDataV2 calibrationFftData;

        string[] audioFrameworks = { "dshow", "alsa", "openal", "oss" };

        string ffmpegAudioRecordingExtension = "wav";
        string ffmpegAudioRecordingParameters = "-c:a pcm_s16le -ac 1 -ar 44100";
        string ffmpegAudioEncodingExtension = "mp3";
        string ffmpegAudioEncodingParameters = "-c:a mp3 -ac 1 -ar 44100 -q:a 9 -filter:a loudnorm";

        float[] selfCalibrationAmplitudes = new float[] { 0.01f, 0.1f, 0.5f, 0.75f, 1.0f };
        float[] selfCalibrationFrequencies = new float[] { 0.1234f, 1.234f, 123.4f, 12340f, 123400f };
        int[] selfCalibrationFFTSizes = new int[] { 1 * 1024, 8 * 1024, 64 * 1024, 256 * 1024, 8192 * 1024 };

        float inputAmplification = short.MaxValue / 1.0f;        // WAV file ranges from -1000 mV to +1000 mV

        DriveInfo spaceCheckDrive;

        List<float> maxValues = new List<float>();

        Pen[] chartPens;
        List<string> disabledIndicators = new List<string>();
        RealFft fft;

        HttpClient httpClient = new HttpClient();

        public Helpers(ILogger logger, BodyMonitorConfig config)
        {
            _logger = logger;
            _config = config;

            foreach (var d in DriveInfo.GetDrives())
            {
                if (Path.GetFullPath(AppendDataDir("")).ToLower().StartsWith(d.RootDirectory.FullName.ToLower()) && ((spaceCheckDrive == null) || (d.Name.Length > spaceCheckDrive.Name.Length)))
                {
                    spaceCheckDrive = d;
                }
            }

            fft = new RealFft(config.Postprocessing.FFTSize);
        }

        public void PreprareMachineLearningModels()
        {
            _logger.LogInformation("Preparing machine learning models, please wait...");
            var dummnyFftData = new FftDataV2()
            {
                FirstFrequency = 0,
                LastFrequency = Single.MaxValue,
                FftSize = _config.Postprocessing.FFTSize,
                MagnitudeData = new float[_config.Postprocessing.FFTSize]
            };
            EvaluateIndicators(_logger, 0, dummnyFftData);
        }

        public void StartDataAcquisition()
        {
            System.Timers.Timer checkDiskSpaceTimer = new System.Timers.Timer(10000);
            checkDiskSpaceTimer.Elapsed += CheckDiskSpaceTimer_Elapsed;
            checkDiskSpaceTimer.AutoReset = true;
            checkDiskSpaceTimer.Enabled = true;


            dataAcquisition = new DataAcquisition(_logger);
            dataAcquisition.SamplesReceived += DataAcquisition_SamplesReceived;
            dataAcquisition.OpenDevice(_config.AD2.Samplerate, _config.AD2.SignalGenerator.Enabled, _config.AD2.SignalGenerator.Channel, _config.AD2.SignalGenerator.Frequency, _config.AD2.SignalGenerator.Voltage);
            dataAcquisition.MinimumSamplesToReceive = (int)Math.Floor(dataAcquisition.Samplerate * _config.Postprocessing.IntervalSeconds);
            _logger.LogInformation($"Recording data at {SimplifyNumber(dataAcquisition.Samplerate)}Hz" + (_config.Postprocessing.Enabled ? $" with the effective FFT resolution of {((dataAcquisition.Samplerate / 2.0) / (_config.Postprocessing.FFTSize / 2)).ToString("0.00")} Hz" : "") + ", press Ctrl+C to stop...");
            dataAcquisition.Start();
        }

        public void CalibrateDevice()
        {
            calibration = new DataAcquisition(_logger);
            calibration.SamplesReceived += Calibration_SamplesReceived;

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

                calibration.OpenDevice(samplerate, true, 1, signalFrequency, signalAmplitude);
                calibration.MinimumSamplesToReceive = fftSize * 2;

                calibrationFftData = new FftDataV2()
                {
                    CaptureTime = DateTime.UtcNow,
                    FirstFrequency = 0,
                    LastFrequency = calibration.Samplerate / 2,
                    FftSize = fftSize,
                };

                calibration.Start();
                calibration.CloseDevice();

                var maxMagnitude = calibrationFftData.MagnitudeData.Max();
                var maxIndex = Array.FindIndex(calibrationFftData.MagnitudeData, md => md == maxMagnitude);
                frequencyErrors[ai] = (calibrationFftData.FirstFrequency + (maxIndex * calibrationFftData.FrequencyStep) - signalFrequency) * 1000;
                amplitudeErrors[ai] = (maxMagnitude - signalAmplitude) * 1000;
            }
            _logger.LogInformation($"Errors are ({String.Join(" | ", amplitudeErrors.Select(e => e.ToString("0.0").PadLeft(7)))}) mV and ({String.Join(" | ", frequencyErrors.Select(e => e.ToString("0.0").PadLeft(9)))}) mHz at ({String.Join(" | ", selfCalibrationAmplitudes.Select(a => a.ToString().PadLeft(7)))}) V amplitudes respectively.");

            signalAmplitude = selfCalibrationAmplitudes[2];
            for (int fi = 0; fi < selfCalibrationFrequencies.Length; fi++)
            {
                signalFrequency = selfCalibrationFrequencies[fi];

                calibration.OpenDevice(samplerate, true, 1, signalFrequency, signalAmplitude);
                calibration.MinimumSamplesToReceive = fftSize * 2;

                calibrationFftData = new FftDataV2()
                {
                    CaptureTime = DateTime.UtcNow,
                    FirstFrequency = 0,
                    LastFrequency = calibration.Samplerate / 2,
                    FftSize = fftSize,
                };

                calibration.Start();
                calibration.CloseDevice();

                var maxMagnitude = calibrationFftData.MagnitudeData.Max();
                var maxIndex = Array.FindIndex(calibrationFftData.MagnitudeData, md => md == maxMagnitude);
                frequencyErrors[fi] = (calibrationFftData.FirstFrequency + (maxIndex * calibrationFftData.FrequencyStep) - signalFrequency) * 1000;
                amplitudeErrors[fi] = (maxMagnitude - signalAmplitude) * 1000;
            }
            _logger.LogInformation($"Errors are ({String.Join(" | ", amplitudeErrors.Select(e => e.ToString("0.0").PadLeft(7)))}) mV and ({String.Join(" | ", frequencyErrors.Select(e => e.ToString("0.0").PadLeft(9)))}) mHz at ({String.Join(" | ", selfCalibrationFrequencies.Select(a => a.ToString().PadLeft(7)))}) Hz respectively.");

            signalFrequency = selfCalibrationFrequencies[2];
            for (int si = 0; si < selfCalibrationFFTSizes.Length; si++)
            {
                fftSize = selfCalibrationFFTSizes[si];

                calibration.OpenDevice(samplerate, true, 1, signalFrequency, signalAmplitude);
                calibration.MinimumSamplesToReceive = fftSize * 2;

                calibrationFftData = new FftDataV2()
                {
                    CaptureTime = DateTime.UtcNow,
                    FirstFrequency = 0,
                    LastFrequency = calibration.Samplerate / 2,
                    FftSize = fftSize,
                };

                calibration.Start();
                calibration.CloseDevice();

                var maxMagnitude = calibrationFftData.MagnitudeData.Max();
                var maxIndex = Array.FindIndex(calibrationFftData.MagnitudeData, md => md == maxMagnitude);
                frequencyErrors[si] = (calibrationFftData.FirstFrequency + (maxIndex * calibrationFftData.FrequencyStep) - signalFrequency) * 1000;
                amplitudeErrors[si] = (maxMagnitude - signalAmplitude) * 1000;
            }
            _logger.LogInformation($"Errors are ({String.Join(" | ", amplitudeErrors.Select(e => e.ToString("0.0").PadLeft(7)))}) mV and ({String.Join(" | ", frequencyErrors.Select(e => e.ToString("0.0").PadLeft(9)))}) mHz at ({String.Join(" | ", selfCalibrationFFTSizes.Select(a => a.ToString().PadLeft(7)))}) FFT sizes respectively.");
        }

        public void Calibration_SamplesReceived(object sender, SamplesReceivedEventArgs e)
        {
            if (e.BufferError)
            {
                _logger.LogError($"There was a data acquisition error, probably because of the sampling rate of {calibration.Samplerate:N0} is too high.");
                return;
            }

            // do an FFT on the samples
            var signal = new DiscreteSignal((int)calibration.Samplerate, e.Samples, true);

            calibrationFftData.Duration = e.Samples.Length / calibration.Samplerate;

            var fft = new RealFft(calibrationFftData.FftSize);
            try
            {
                calibrationFftData.MagnitudeData = fft.MagnitudeSpectrum(signal, normalize: true).Samples[1..];
            }
            catch (IndexOutOfRangeException)
            {
                _logger.LogError($"The FFT size of {calibrationFftData.FftSize:N0} is too high for the sample rate of {calibration.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                return;
            }

            calibrationFftData.BasedOnSamplesCount = e.Samples.Length;
            calibrationFftData.FrequencyStep = ((float)calibrationFftData.LastFrequency) / (calibrationFftData.MagnitudeData.Length - 1);
            calibration.Stop();
        }

        public void CheckDiskSpaceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((spaceCheckDrive != null) && (spaceCheckDrive.AvailableFreeSpace < _config.MinimumAvailableFreeSpace))
            {
                _logger.LogError($"There is not enough space on drive {spaceCheckDrive.Name}.  to continue. There must be at least {_config.MinimumAvailableFreeSpace:N0} bytes of available free space at all times.");
                dataAcquisition.Stop();
            }
        }

        public void DataAcquisition_SamplesReceived(object sender, SamplesReceivedEventArgs e)
        {
            DateTime captureTime = DateTime.UtcNow.AddSeconds(-_config.Postprocessing.IntervalSeconds);
            string pathToFile = GeneratePathToFile(captureTime);

            if ((_config.Postprocessing.Enabled) && (!e.BufferError))
            {
                Task.Run(() =>
                {
                    string threadId = e.BufferIndex.ToString("0000");

                    //generate an NWaves signal
                    int sampleCount = e.Samples.Length;
                    float[] samplesToAdd = e.Samples;
                    if (sampleCount < _config.Postprocessing.FFTSize)
                    {
                        var samplesToAddList = e.Samples.ToList();
                        samplesToAddList.AddRange(Enumerable.Repeat(0.0f, _config.Postprocessing.FFTSize - sampleCount));
                        samplesToAdd = samplesToAddList.ToArray();
                    }
                    var signal = new DiscreteSignal((int)dataAcquisition.Samplerate, samplesToAdd, true);

                    _logger.LogTrace($"Postprocessing thread #{threadId} begin");

                    var fftData = new FftDataV2()
                    {
                        CaptureTime = captureTime,
                        Duration = _config.Postprocessing.IntervalSeconds,
                        BasedOnSamplesCount = sampleCount,
                        FirstFrequency = 0,
                        LastFrequency = dataAcquisition.Samplerate / 2,
                        FftSize = _config.Postprocessing.FFTSize,
                    };

                    Stopwatch sw = Stopwatch.StartNew();

                    if (_config.Postprocessing.SaveAsWAV)
                    {
                        sw.Restart();
                        // and save samples to a WAV file
                        FileStream waveFileStream = new FileStream(AppendDataDir($"{pathToFile}.wav"), FileMode.Create);
                        DiscreteSignal signalToSave = new DiscreteSignal(signal.SamplingRate, signal.Samples.Take(sampleCount).ToArray(), true);
                        signalToSave.Amplify(inputAmplification);
                        WaveFile waveFile = new WaveFile(signalToSave, 16);
                        waveFile.SaveTo(waveFileStream, false);

                        waveFileStream.Close();

                        sw.Stop();
                        _logger.LogTrace($"#{threadId} Save as WAV completed in {sw.ElapsedMilliseconds:N0} ms.");
                    }

                    sw.Restart();
                    try
                    {
                        //logger.LogInformation($"#{bi.ToString("0000")} signal.Samples.Length: {sampleCount:N0} | FFT size: {config.Postprocessing.FFTSize:N0}");

                        // calculate magnitude spectrum with normalization and ignore the 0th coefficient (DC component)
                        fftData.MagnitudeData = fft.MagnitudeSpectrum(signal, normalize: true).Samples[1..];

                        fftData.FrequencyStep = fftData.LastFrequency / (fftData.MagnitudeData.Length - 1);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        _logger.LogError($"The FFT size of {_config.Postprocessing.FFTSize:N0} is too high for the sample rate of {_config.AD2.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                        dataAcquisition.Stop();
                        return;
                    }
                    sw.Stop();
                    _logger.LogTrace($"#{threadId} Signal processing completed in {sw.ElapsedMilliseconds:N0} ms.");


                    MagnitudeStats magnitudeStats = fftData.GetMagnitudeStats();

                    maxValues.Add(magnitudeStats.Max);
                    int maxIndexStart = (int)Math.Max(0, magnitudeStats.MaxIndex - 2);
                    int maxIndexEnd = (int)Math.Min(fftData.MagnitudeData.Length - 1, magnitudeStats.MaxIndex + 2 + 1);

                    _logger.LogTrace($"#{threadId} The maximum magnitude values are ( {String.Join(" | ", fftData.MagnitudeData[maxIndexStart..maxIndexEnd].Select(m => String.Format("{0,10:N}", m * 1000 * 1000)))} ) µV around {fftData.GetBinFromIndex(magnitudeStats.MaxIndex)}.");

                    if (_config.AD2.SignalGenerator.Enabled)
                    {
                        int fftDataIndex = (int)((_config.AD2.SignalGenerator.Frequency - fftData.FirstFrequency) / fftData.FrequencyStep);
                        int fftDataIndexStart = fftDataIndex - 2;
                        int fftDataIndexEnd = fftDataIndex + 2 + 1;

                        if ((fftDataIndex - 1 < 0) || (fftDataIndex + 1 > fftData.MagnitudeData.Length - 1))
                        {
                            _logger.LogWarning($"#{threadId:0000} The signal generator generates a signal at {_config.AD2.SignalGenerator.Frequency:N} Hz and that is out of the range of the current sampling ({fftData.FirstFrequency:N} Hz - {fftData.LastFrequency:N} Hz).");
                        }
                        else
                        {
                            _logger.LogInformation($"#{threadId:0000} The magnitude values around {_config.AD2.SignalGenerator.Frequency} Hz are ({String.Join(" | ", fftData.MagnitudeData[fftDataIndexStart..fftDataIndexEnd].Select(m => String.Format("{0,7:N}", m * 1000 * 1000)))}) µV.");
                        }
                    }

                    //if (config.Postprocessing.MagnitudeThreshold > 0)
                    //{
                    //    float averageMagnitude = fftData.MagnitudesPer1p0Hz[100..1000].Average();
                    //    if (averageMagnitude < config.Postprocessing.MagnitudeThreshold)
                    //    {
                    //        logger.LogWarning($"#{bi.ToString("0000")} The average magnitude in the 100-1000 Hz range is too low: {averageMagnitude * 1000 * 1000:N0} µV. The threashold is {config.Postprocessing.MagnitudeThreshold * 1000 * 1000:N0} µV.");
                    //        return;
                    //    }
                    //}

                    FftDataV2 resampledFFTData = fftData.Downsample(_config.Postprocessing.ResampleFFTResolutionToHz);
                    string startFrequency = (1 * resampledFFTData.FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    string endFrequency = ((resampledFFTData.MagnitudeData.Length - 1) * resampledFFTData.FrequencyStep).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                    string fftRange = ($"{startFrequency}Hz-{endFrequency}Hz").Replace(".", "p");
                    if (_config.Postprocessing.SaveAsFFT)
                    {
                        Task.Run(() =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();

                            //save the FFT to a JSON file
                            sw.Restart();
                            FftDataV2.SaveAs(resampledFFTData, AppendDataDir($"{pathToFile}__{fftRange}.fft"), false);
                            sw.Stop();
                            _logger.LogTrace($"#{threadId} Save as FFT completed in {sw.ElapsedMilliseconds:N0} ms.");
                        });
                    }

                    if (_config.Postprocessing.SaveAsCompressedFFT)
                    {
                        Task.Run(() =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();

                            //save the FFT to a zipped JSON file
                            sw.Restart();
                            FftDataV2.SaveAs(resampledFFTData, AppendDataDir($"{pathToFile}__{fftRange}.zip"), true);
                            sw.Stop();
                            _logger.LogTrace($"#{threadId} Save as compressed FFT completed in {sw.ElapsedMilliseconds:N0} ms.");
                        });
                    }

                    if (_config.Postprocessing.SaveAsBinaryFFT)
                    {
                        Task.Run(() =>
                        {
                            Stopwatch sw = Stopwatch.StartNew();

                            //save the FFT to a binary file
                            sw.Restart();
                            FftDataV2.SaveAsBinary(resampledFFTData, AppendDataDir($"{pathToFile}__{fftRange}.fft"), false);
                            sw.Stop();
                            _logger.LogTrace($"#{threadId} Save as binary FFT completed in {sw.ElapsedMilliseconds:N0} ms.");
                        });
                    }


                    if (_config.Postprocessing.SaveAsPNG.Enabled)
                    {
                        var mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => !String.IsNullOrWhiteSpace(_config.Postprocessing.SaveAsPNG.MLProfile) && p.Name.StartsWith(_config.Postprocessing.SaveAsPNG.MLProfile));
                        if (mlProfile == null)
                        {
                            _logger.LogError($"The profile '{_config.Postprocessing.SaveAsPNG.MLProfile}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                            _config.Postprocessing.SaveAsPNG.Enabled = false;
                        }
                        else
                        {
                            Task.Run(() =>
                            {
                                Stopwatch sw = Stopwatch.StartNew();

                                //save a PNG with the values
                                sw.Restart();
                                string filenameComplete = $"{pathToFile}_{SimplifyNumber(mlProfile.MaxFrequency)}Hz_{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY)}V.png";
                                SaveSignalAsPng(AppendDataDir(filenameComplete), fftData, _config.Postprocessing.SaveAsPNG, mlProfile);
                                sw.Stop();
                                _logger.LogTrace($"#{threadId} Save as PNG completed in {sw.ElapsedMilliseconds:N0} ms.");
                            });
                        }
                    }
                    if (_config.Indicators.Enabled)
                    {
                        var evaluationResults = EvaluateIndicators(_logger, e.BufferIndex, resampledFFTData);

                        if (evaluationResults.Length > 0)
                        {
                            _logger.LogInformation($"#{threadId} {resampledFFTData.CaptureTime:HH:mm:ss}-{(resampledFFTData.CaptureTime.AddSeconds(_config.Postprocessing.IntervalSeconds)):HH:mm:ss} {String.Join(" | ", evaluationResults.Select(er => er.DisplayText + " " + er.PredictionScore.ToString("+0.0%;-0.0%; 0.0%").PadLeft(7)))}.");
                        }
                    }

                    _logger.LogTrace($"Postprocessing thread #{threadId} end");
                }
                );
            }

            if (_config.AudioRecording.Enabled)
            {
                Task.Run(() =>
                {
                    TimeSpan tp = captureTime.Add(new TimeSpan(0, 0, (int)_config.Postprocessing.IntervalSeconds * 2)) - DateTime.UtcNow;

                    string pathToAudioFile = GeneratePathToFile(DateTime.UtcNow);

                    string recFilename = AppendDataDir($"{pathToAudioFile}.{ffmpegAudioRecordingExtension}");

                    string ffmpegAudioFramework = _config.AudioRecording.PreferredDevice.Split("/")[0];
                    string ffmpegAudioDevice = _config.AudioRecording.PreferredDevice.Split("/")[1];
                    string audioRecordingCommandLine = $"-f {ffmpegAudioFramework} -ac 1 -i audio=\"{ffmpegAudioDevice}\" {ffmpegAudioRecordingParameters} -t {tp:mm':'ss'.'fff} \"{recFilename}\"";
                    _logger.LogDebug($"ffmpeg {audioRecordingCommandLine}");

                    try
                    {
                        FFmpeg.Conversions.New().Start(audioRecordingCommandLine)
                            .ContinueWith((Task<IConversionResult> cr) =>
                            {
                                try
                                {
                                    Stopwatch sw = Stopwatch.StartNew();
                                    sw.Restart();

                                    float waveRms = 0;
                                    float wavePeak = 0;
                                    using (var waveStream = new FileStream(recFilename, FileMode.Open))
                                    {
                                        DiscreteSignal recordedAudio = new WaveFile(waveStream).Signals[0];
                                        waveRms = recordedAudio.Rms();
                                        wavePeak = Math.Max(-recordedAudio.Samples.Min(), recordedAudio.Samples.Max()) * 100;
                                    }

                                    //if (waveRms >= config.AudioRecording.SilenceThreshold)
                                    if (wavePeak >= _config.AudioRecording.SilenceThreshold)
                                    {
                                        string finalFilename = AppendDataDir($"{pathToAudioFile}_{wavePeak.ToString("00.0")}%.{ffmpegAudioEncodingExtension}");
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
            var ffmpegAudioNames = new List<string>();

            ffmpegAudioNames.Clear();
            foreach (string audioFramework in audioFrameworks)
            {
                try
                {
                    string listDevicesCommandLine = $"-list_devices true -f {audioFramework} -i dummy";
                    _logger.LogDebug($"ffmpeg {listDevicesCommandLine}");
                    var listDevicesConversion = FFmpeg.Conversions.New();
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

                                if (audioDeviceDetails.Contains("Alternative name") || !audioDeviceDetails.Contains("\"")) continue;

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
                _logger.LogWarning($"The preferred audio input device was not listed by the operating system as a valid audio source. The listed audio inputs are: {String.Join(", ", ffmpegAudioNames)}.");
            }

            return ffmpegAudioNames;
        }

        public IndicatorEvaluationResult[] EvaluateIndicators(ILogger logger, int blockIndex, FftDataV2 inputData)
        {
            List<IndicatorEvaluationResult> result = new List<IndicatorEvaluationResult>();

            IndicatorEvaluationTaskDescriptor[] taskDescriptors = new IndicatorEvaluationTaskDescriptor[]
            {
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 0,
                //    IndicatorName = "IsSubject_None",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP05")),
                //    MLModelType = typeof(BBD_BodyMonitor.BBD_20220530_TrainingData_MLP05_0p5Hz_200Hz_IsSubject_None),
                //    DisplayText = "Not attached?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 1,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP05")),
                //    MLModelType = typeof(BBD_BodyMonitor.BBD_20220530_TrainingData_MLP05_0p5Hz_200Hz_IsSubject_AndrasFuchs),
                //    DisplayText = "Andris (old MLP05)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 2,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP05")),
                //    MLModelType = typeof(BBD_BodyMonitor_CLI.BBD_20220829_TrainingData_MLP05_0p5Hz_200Hz_IsSubject_AndrasFuchs),
                //    DisplayText = "Andris (new MLP05)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 3,
                //    IndicatorName = "IsSubject_AndrasFuchs",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP08")),
                //    MLModelType = typeof(BBD_BodyMonitor_CLI.BBD_20220829_TrainingData_MLP08_10p0Hz_150000Hz_IsSubject_AndrasFuchs),
                //    DisplayText = "Andris (MLP08)?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 4,
                //    IndicatorName = "IsSubject_TimeaNagy",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP05")),
                //    MLModelType = typeof(BBD_BodyMonitor.BBD_20220530_TrainingData_MLP05_0p5Hz_200Hz_IsSubject_TimeaNagy),
                //    DisplayText = "Timi?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 5,
                //    IndicatorName = "IsSubject_TimeaNagy",
                //    MLProfile = config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP03")),
                //    MLModelType = typeof(BBD_20220228_TrainingData_MLP03_5p0Hz_99500Hz_IsSubject_TimeaNagy)
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 6,
                //    IndicatorName = "IsActivity_Meditation",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP05")),
                //    MLModelType = typeof(BBD_BodyMonitor.BBD_20220531_TrainingData_MLP05_0p5Hz_200Hz_IsActivity_Meditation),
                //    DisplayText = "Meditating?"
                //}
            };

            inputData.Load();

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var tasks = new List<Task>();
            foreach (IndicatorEvaluationTaskDescriptor td in taskDescriptors)
            {
                var task = Task.Run(() =>
                {
                    result.Add(EvaluateIndicatorWithReflection(blockIndex, td.IndicatorIndex, td.IndicatorName, td.DisplayText, td.MLModelType, inputData.ApplyMLProfile(td.MLProfile)));
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            sw.Stop();
            logger.LogTrace($"#{blockIndex:0000} Predictions were completed in {sw.ElapsedMilliseconds:N0} ms.");

            return result.OrderBy(r => r.IndicatorIndex).ToArray();
        }

        public IndicatorEvaluationResult EvaluateIndicatorWithReflection(int blockIndex, int indicatorIndex, string indicatorName, string displayText, Type mlModelType, FftDataV2 inputData)
        {
            var mlModelInputType = mlModelType.GetNestedType("ModelInput");
            var mlModelOutputType = mlModelType.GetNestedType("ModelOutput");

            var modelInput = Activator.CreateInstance(mlModelInputType);
            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < inputData.MagnitudeData.Length; i++)
            {
                string propertyName = $"Freq_{inputData.GetBinFromIndex(i).ToString("0.00")}_Hz".Replace(".", "p");
                System.Reflection.PropertyInfo freqProperyInfo = modelInput.GetType().GetProperty(propertyName);

                if (freqProperyInfo == null)
                {
                    _logger.LogWarning($"Property '{propertyName}' was not found on the ML model input class of '{indicatorName}'.");
                    disabledIndicators.Add(indicatorName);
                    break;
                }

                freqProperyInfo.SetValue(modelInput, inputData.MagnitudeData[i]);
            }
            sw.Stop();
            _logger.LogTrace($"#{blockIndex:0000} Input data setting was completed in {sw.ElapsedMilliseconds:N0} ms.");

            sw.Restart();
            var modelOutput = mlModelType.GetMethod("Predict").Invoke(null, new object[] { modelInput });
            float modelOutputValue = (float)mlModelOutputType.GetProperty(indicatorName).GetValue(modelOutput);
            //float modelOutputValue = (bool)mlModelOutputType.GetProperty(indicatorName).GetValue(modelOutput) ? 1.0f : 0.0f;
            float modelOutputScore = (float)mlModelOutputType.GetProperty("Score").GetValue(modelOutput);
            //float modelOutputScore = (float)mlModelOutputType.GetProperty("Probability").GetValue(modelOutput);
            bool hasRelevantTag = inputData.Tags?.Any(t => indicatorName[2..] == t) ?? false;
            bool hasPredictedTag = modelOutputScore > 0.5;
            sw.Stop();
            _logger.LogTrace($"#{blockIndex:0000} Prediction was completed in {sw.ElapsedMilliseconds:N0} ms.");

            return new IndicatorEvaluationResult()
            {
                BlockIndex = blockIndex,
                IndicatorIndex = indicatorIndex,
                IndicatorName = indicatorName,
                DisplayText = displayText,
                Value = modelOutputValue,
                PredictionScore = modelOutputScore,
                IsTruePositive = hasRelevantTag && hasPredictedTag,
                IsTrueNegative = !hasRelevantTag && !hasPredictedTag,
                IsFalsePositive = !hasRelevantTag && hasPredictedTag,
                IsFalseNegative = hasRelevantTag && !hasPredictedTag
            };
        }

        public IndicatorEvaluationResult EvaluateIndicatorWithDirectSetting(int blockIndex, int indicatorIndex, string indicatorName, Type mlModelType, FftDataV2 inputData)
        {
            //Create MLContext
            MLContext mlContext = new MLContext();

            // Load Trained Model
            DataViewSchema predictionPipelineSchema;
            ITransformer predictionPipeline = mlContext.Model.Load("BBD_20220312_TrainingData_MLP03_5p0Hz-99500Hz_IsSubject_AndrasFuchs.zip", out predictionPipelineSchema);

            // Create PredictionEngines
            PredictionEngine<FftModelInput, FftModelOutput> predictionEngine = mlContext.Model.CreatePredictionEngine<FftModelInput, FftModelOutput>(predictionPipeline);

            // Input Data
            Stopwatch sw = Stopwatch.StartNew();
            FftModelInput fftModelInput = new FftModelInput()
            {
                MagnitudeData = inputData.MagnitudeData
            };
            //var dataView = mlContext.Data.LoadFromEnumerable(fftModelInput.MagnitudeData);
            sw.Stop();
            _logger.LogTrace($"#{blockIndex:0000} Input data setting was completed in {sw.ElapsedMilliseconds:N0} ms.");

            // Get Prediction
            sw.Restart();
            FftModelOutput fftModelOutput = predictionEngine.Predict(fftModelInput);

            float modelOutputValue = fftModelOutput.Scores[0];
            float modelOutputScore = fftModelOutput.Scores[1];

            bool hasRelevantTag = inputData.Tags?.Any(t => indicatorName[2..] == t) ?? false;
            bool hasPredictedTag = modelOutputScore > 0.5;
            sw.Stop();
            _logger.LogTrace($"#{blockIndex:0000} Prediction was completed in {sw.ElapsedMilliseconds:N0} ms.");

            return new IndicatorEvaluationResult()
            {
                BlockIndex = blockIndex,
                IndicatorIndex = indicatorIndex,
                IndicatorName = indicatorName,
                Value = modelOutputValue,
                PredictionScore = modelOutputScore,
                IsTruePositive = hasRelevantTag && hasPredictedTag,
                IsTrueNegative = !hasRelevantTag && !hasPredictedTag,
                IsFalsePositive = !hasRelevantTag && hasPredictedTag,
                IsFalseNegative = hasRelevantTag && !hasPredictedTag
            };
        }

        public string GeneratePathToFile(DateTime captureTime)
        {
            if (!Directory.Exists(AppendDataDir(captureTime.ToString("yyyy-MM-dd"))))
            {
                Directory.CreateDirectory(AppendDataDir(captureTime.ToString("yyyy-MM-dd")));
            }

            string foldername = $"{captureTime.ToString("yyyy-MM-dd")}";
            string filename = $"AD2_{captureTime.ToString("yyyyMMdd_HHmmss")}";
            return Path.Combine(foldername, filename);
        }

        public void GenerateVideo(string foldername, MLProfile mlProfile)
        {
            foreach (FftDataV2 fftData in EnumerateFFTDataInFolder(foldername))
            {
                try
                {
                    string filenameComplete = Path.Combine(foldername, $"{fftData.Name}_{SimplifyNumber(mlProfile.MaxFrequency)}Hz_{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY)}V.png");

                    if (File.Exists(AppendDataDir(filenameComplete)))
                    {
                        _logger.LogWarning($"{filenameComplete} already exists.");
                        continue;
                    }

                    SaveSignalAsPng(AppendDataDir(filenameComplete), fftData, _config.Postprocessing.SaveAsPNG, mlProfile);
                    _logger.LogInformation($"{filenameComplete} was generated successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"There was an error while generating PNG for '{fftData.Name}': {ex.Message}");
                }
            }

            string mp4FilenameBase = foldername.Replace("\\", "_").Replace("#", "_");
            string mp4Filename = $"BBD_{mp4FilenameBase}_{SimplifyNumber(mlProfile.MaxFrequency)}Hz_{SimplifyNumber(_config.Postprocessing.SaveAsPNG.RangeY)}V.mp4";
            _logger.LogInformation($"Generating MP4 video file '{mp4Filename}'");
            try
            {
                FFmpeg.Conversions.New()
                    .SetInputFrameRate(12)
                    .BuildVideoFromImages(Directory.GetFiles(AppendDataDir(foldername), "*.png").OrderBy(fn => fn))
                .SetFrameRate(12)
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

        public void GenerateMLCSV(string foldername, MLProfile mlProfile, bool includeHeaders, string tagFilterExpression, string validLabelExpression)
        {
            HashSet<string> validTags = new HashSet<string>();
            string[] requiredTags = tagFilterExpression.Split("||", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            string[] validLabels = validLabelExpression.Split("+", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();
            List<FftDataV2> fftDataCache = new List<FftDataV2>();
            List<FftDataV2> downsampledFFTDataCache = new List<FftDataV2>();
            int featureColumnIndexStart = 0;
            int featureColumnIndexEnd = 0;
            DateTimeOffset nextConsoleFeedback = DateTime.UtcNow;

            foldername = AppendDataDir(foldername);
            if (!foldername.EndsWith(Path.DirectorySeparatorChar))
            {
                foldername += Path.DirectorySeparatorChar;
            }

            if (Directory.Exists(foldername))
            {
                FftDataV2 templateFftData = null;

                int fftDataCount = 0;
                foreach (FftDataV2 fftData in EnumerateFFTDataInFolder(foldername))
                {
                    fftDataCache.Add(fftData);
                    fftDataCount++;
                }


                Stopwatch sw = new Stopwatch();
                sw.Start();

                int fftDataLoadCompleteCount = 0;
                long fftDataBytesLoaded = 0;

                Task.Run(() =>
                {
                    while (fftDataLoadCompleteCount < fftDataCount)
                    {
                        if (fftDataBytesLoaded > 0)
                        {
                            _logger.LogInformation($"{(((float)fftDataLoadCompleteCount / fftDataCount) * 100.0f).ToString("0.0")}% of FFT data was loaded with the effective speed of {((float)fftDataBytesLoaded / (1024 * 1024)) / ((float)sw.ElapsedMilliseconds / 1000):0.0} MB/s.");
                        }
                        Thread.Sleep(1000);
                    }
                });

                var tasks = new List<Task>();
                foreach (FftDataV2 fftData in fftDataCache)
                {
                    var task = Task.Run(() =>
                    {
                        fftData.Load();
                        fftDataLoadCompleteCount++;
                        fftDataBytesLoaded += fftData.FileSize;

                        if (!String.IsNullOrWhiteSpace(tagFilterExpression))
                        {
                            if (!requiredTags.Any(rt => fftData.Tags.Any(t => t.Contains(rt)))) return;
                        }

                        foreach (string tag in fftData.Tags)
                        {
                            validTags.Add(tag);
                        }

                        var downsampledFFTData = fftData.ApplyMLProfile(mlProfile);
                        lock (downsampledFFTDataCache)
                        {
                            downsampledFFTDataCache.Add(downsampledFFTData);
                        }

                        fftData.Clear();
                    });
                    tasks.Add(task);
                }
                Task.WaitAll(tasks.ToArray());
                sw.Stop();
                fftDataLoadCompleteCount = fftDataCount;
                _logger.LogInformation($"{(((float)fftDataLoadCompleteCount / fftDataCount) * 100.0f).ToString("0.0")}% of FFT data was loaded in {sw.ElapsedMilliseconds:N0} ms with the effective speed of {((float)fftDataBytesLoaded / (1024 * 1024)) / ((float)sw.ElapsedMilliseconds / 1000):0.0} MB/s.");

                fftDataCache.Clear();

                templateFftData = downsampledFFTDataCache[0];
                featureColumnIndexStart = 0;
                featureColumnIndexEnd = downsampledFFTDataCache[0].MagnitudeData.Length;

                if (validLabels.Length == 0)
                {
                    validLabels = validTags.ToArray();
                }

                string[] featureColumnNames = Enumerable.Range(featureColumnIndexStart, featureColumnIndexEnd - featureColumnIndexStart).Select(i => "Freq_" + templateFftData.GetBinFromIndex(i).ToString("0.00").Replace(".", "p") + "_Hz").ToArray();
                string[] labelColumnNames = validLabels.Select(l => "Is" + l).ToArray();
                if (includeHeaders)
                {
                    string header = String.Join(",", featureColumnNames);
                    header += "," + String.Join(",", labelColumnNames);

                    sb.AppendLine(header);
                }

                string filenameRoot = $"BBD_{downsampledFFTDataCache.Max(d => d.FileModificationTime).ToString("yyyyMMdd")}_{Path.GetFileName(Path.GetDirectoryName(foldername))}_{mlProfile.Name}" + (validLabels.Length == 1 ? "_Is" + validLabels[0] : "");
                string csvFilename = filenameRoot + ".csv";
                _logger.LogInformation($"Generating machine learning CSV file '{csvFilename}' with {featureColumnIndexEnd - featureColumnIndexStart + validLabels.Length} columns and {fftDataCache.Count + 1} rows.");
                try
                {
                    File.WriteAllText(AppendDataDir(csvFilename), sb.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError($"There was an error while generating headers for CSV file '{csvFilename}': {ex.Message}");
                }
                sb.Clear();

                SortedSet<float> stats = new SortedSet<float>();

                Dictionary<string, List<float>> labelValues = new Dictionary<string, List<float>>();
                int i = 0;
                foreach (FftDataV2 fftData in downsampledFFTDataCache)
                {
                    if (nextConsoleFeedback < DateTime.UtcNow)
                    {
                        _logger.LogInformation($"Adding '{fftData.Name}' to the machine learning CSV file. {(((float)i / fftDataCount) * 100.0f).ToString("0.0")}% done.");
                        nextConsoleFeedback = DateTime.UtcNow.AddSeconds(3);

                        try
                        {
                            File.AppendAllText(AppendDataDir(csvFilename), sb.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"There was an error while appending to CSV file '{csvFilename}': {ex.Message}");
                        }
                        sb.Clear();
                    }

                    foreach (float md in fftData.MagnitudeData[featureColumnIndexStart..featureColumnIndexEnd])
                    {
                        stats.Add(md);
                    }

                    sb.Append(String.Join(",", fftData.MagnitudeData[featureColumnIndexStart..featureColumnIndexEnd].Select(md => md.ToString("0.0000000000", System.Globalization.CultureInfo.InvariantCulture.NumberFormat))));
                    foreach (string validLabel in validLabels)
                    {
                        float labelValue = fftData.Tags.Contains(validLabel) ? 1.00f : 0.00f;

                        if (!labelValues.ContainsKey(validLabel))
                        {
                            labelValues.Add(validLabel, new List<float>());
                        }
                        labelValues[validLabel].Add(labelValue);

                        // use this if it's a regression scenario
                        sb.Append("," + labelValue.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture.NumberFormat));

                        // use this if it's a classification scenario
                        //sb.Append("," + (labelValue == 1.0 ? "true" : "false"));
                    }
                    sb.AppendLine();
                    i++;
                }

                try
                {
                    File.AppendAllText(AppendDataDir(csvFilename), sb.ToString());
                }
                catch (Exception ex)
                {
                    _logger.LogError($"There was an error while appending to CSV file '{csvFilename}': {ex.Message}");
                }
                sb.Clear();
                var statsArray = stats.ToArray();

                _logger.LogInformation($"FFT magnitude data stats:");
                for (int j = 0; j < statsArray.Length; j += statsArray.Length / 100)
                {
                    _logger.LogInformation($" the bottom {j / (statsArray.Length / 100)}% is less than {statsArray[j].ToString("0.00000000")}.");
                }

                string mbconfigFilename = filenameRoot + ".mbconfig";
                _logger.LogInformation($"Generating mbconfig file '{mbconfigFilename}'.");
                try
                {
                    MBConfig mbConfig = new MBConfig(AppendDataDir(csvFilename), featureColumnNames, labelColumnNames);
                    string mbConfigJson = JsonSerializer.Serialize(mbConfig, new JsonSerializerOptions() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } });
                    File.WriteAllText(AppendDataDir(mbconfigFilename), mbConfigJson);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"There was an error while generating mbconfig file '{mbconfigFilename}': {ex.Message}");
                }


                if (labelValues.Count > 0)
                {
                    _logger.LogInformation($"Label statistics based on {labelValues.First().Value.Count} entries:");
                    foreach (string validTag in validTags)
                    {
                        if (labelValues.ContainsKey(validTag))
                        {
                            _logger.LogInformation($" #{validTag} ({labelValues[validTag].Count(v => v > 0)}): min = {labelValues[validTag].Min().ToString("0.0000")} | avg = {labelValues[validTag].Average().ToString("0.0000")} | max = {labelValues[validTag].Max().ToString("0.0000")}");
                        }
                    }
                }
            }
        }

        public void TestAllModels(string foldername, float percentageToTest)
        {
            _logger.LogInformation($"Evaluating models based on {percentageToTest}% of the data in the '{foldername}' folder.");

            Random rnd = new Random();
            Dictionary<string, int[]> confusionMatrix = new Dictionary<string, int[]>();
            foldername = AppendDataDir(foldername);

            if (Directory.Exists(foldername))
            {
                int i = 0;
                foreach (FftDataV2 fftData in EnumerateFFTDataInFolder(foldername))
                {
                    if (rnd.NextDouble() > percentageToTest / 100.0) continue;

                    _logger.LogInformation($"Evaluating {fftData.Name}.");
                    var evaluationResults = EvaluateIndicators(_logger, i++, fftData);

                    foreach (var er in evaluationResults)
                    {
                        if (!confusionMatrix.ContainsKey(er.IndicatorName))
                        {
                            confusionMatrix.Add(er.IndicatorName, new int[4]);
                        }

                        if (er.IsTruePositive) confusionMatrix[er.IndicatorName][0]++;
                        if (er.IsFalseNegative) confusionMatrix[er.IndicatorName][1]++;
                        if (er.IsFalsePositive) confusionMatrix[er.IndicatorName][2]++;
                        if (er.IsTrueNegative) confusionMatrix[er.IndicatorName][3]++;
                    }
                }
            }

            _logger.LogInformation($"");
            foreach (var cm in confusionMatrix)
            {
                _logger.LogInformation($"Confusion matrix for the '{cm.Key}' indicator:");
                _logger.LogInformation($"    +--------------------------------+");
                _logger.LogInformation($"    |     Prediction condition       |");
                _logger.LogInformation($"    |----------+----------+----------|");
                _logger.LogInformation($"    |  Total   |          |          |");
                _logger.LogInformation($"    | {String.Format("{0,7}", cm.Value[0] + cm.Value[1] + cm.Value[2] + cm.Value[3])}  | positive | negative |");
                _logger.LogInformation($"+---|----------|----------|----------|");
                _logger.LogInformation($"| a | positive |TP:{String.Format("{0,7}", cm.Value[0])}|FN:{String.Format("{0,7}", cm.Value[1])}|");
                _logger.LogInformation($"| c |----------|----------|----------|");
                _logger.LogInformation($"| t | negative |FP:{String.Format("{0,7}", cm.Value[2])}|TN:{String.Format("{0,7}", cm.Value[3])}|");
                _logger.LogInformation($"+---+----------+----------+----------+");
                _logger.LogInformation($"Accuracy: {((double)(cm.Value[0] + cm.Value[3]) / (double)(cm.Value[0] + cm.Value[1] + cm.Value[2] + cm.Value[3]) * 100).ToString("##0.00")}%");
                _logger.LogInformation($"");
            }

        }
        public IEnumerable<FftDataV2> EnumerateFFTDataInFolder(string foldername, string[] applyTags = null)
        {
            if (applyTags == null) applyTags = Array.Empty<string>();

            if (Path.GetFileName(foldername).StartsWith(".")) yield break;

            if (Directory.Exists(AppendDataDir(foldername)))
            {
                foreach (string filename in Directory.GetFiles(AppendDataDir(foldername)).OrderBy(n => n))
                {
                    string pathToFile = Path.GetFullPath(filename);

                    if ((Path.GetExtension(filename) != ".bfft") && (Path.GetExtension(filename) != ".fft") && (Path.GetExtension(filename) != ".zip"))
                    {
                        continue;
                    }

                    FftDataV2 fftData = null;
                    try
                    {
                        var fi = new FileInfo(pathToFile);

                        fftData = new FftDataV2();
                        fftData.Filename = pathToFile;
                        fftData.FileSize = fi.Length;
                        fftData.FileModificationTime = fi.LastWriteTime;
                        fftData.Name = Path.GetFileNameWithoutExtension(pathToFile);
                        fftData.Tags = applyTags;
                    }
                    catch
                    {
                    }

                    if (fftData != null)
                    {
                        yield return fftData;
                    }
                }

                foreach (string directoryName in Directory.GetDirectories(AppendDataDir(foldername)).OrderBy(n => n))
                {
                    var tags = new List<string>(applyTags);
                    string newTag = Path.GetFileName(directoryName);
                    if (newTag.StartsWith("#"))
                    {
                        tags.Add(newTag[1..]);
                    }

                    foreach (var fftData in EnumerateFFTDataInFolder(directoryName, tags.ToArray()))
                    {
                        yield return fftData;
                    }
                }
            }
        }

        public string AppendDataDir(string filename)
        {
            if (!String.IsNullOrWhiteSpace(_config.DataDirectory))
            {
                return Path.Combine(_config.DataDirectory, filename);
            }
            else
            {
                return Path.Combine(Directory.GetCurrentDirectory(), filename);
            }
        }

        public void SaveSignalAsPng(string filename, FftDataV2 fftData, SaveAsPngConfig config, MLProfile mlProfile)
        {
            Size targetResolution = new Size(config.TargetWidth, config.TargetHeight);
            float idealAspectRatio = (float)targetResolution.Width / (float)targetResolution.Height;

            fftData.Load();
            fftData = fftData.ApplyMLProfile(mlProfile);
            string tags = String.Join(", ", fftData.Tags ?? Array.Empty<string>());

            float maxValue = fftData.MagnitudeData.Max();
            int maxValueAtIndex = Array.IndexOf(fftData.MagnitudeData, maxValue);

            int sampleCount;

            int samplesPerRow;
            do
            {
                sampleCount = fftData.MagnitudeData.Length;
                samplesPerRow = 1;
                while ((samplesPerRow < sampleCount) && (fftData.GetBinFromIndex(samplesPerRow).EndFrequency <= config.RangeX))
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

            Color bgColor = Color.LightGray;

            if (samplesPerRow > 9500)
            {
                throw new Exception($"We supposed to generate a bitmap with {samplesPerRow} pixel width. Width must be less than 9500 pixels.");
            }

            if (rowCount * rowHeight > 55000)
            {
                throw new Exception($"We supposed to generate a bitmap with {rowCount * rowHeight} pixel height. Height must be less than 55000 pixels.");
            }

            Bitmap spectrumBitmap = new Bitmap(samplesPerRow, rowHeight * rowCount, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            Graphics graphics = Graphics.FromImage(spectrumBitmap);
            graphics.FillRectangle(new SolidBrush(bgColor), new Rectangle(0, 0, samplesPerRow, rowHeight * rowCount));

            if (chartPens == null)
            {
                chartPens = new Pen[11];
                for (int i = 0; i <= 10; i++)
                {
                    chartPens[i] = new Pen(new SolidBrush(ColorInterpolator.InterpolateBetween(Color.DarkGray, Color.DarkRed, ((double)i / 10.0) / 2.0)), 1.0f);
                }
            }

            float sampleAmplification = (1.0f / config.RangeY) * rowHeight;

            for (int r = 1; r <= rowCount; r++)
            {
                int bottomLine = r * rowHeight;

                for (int i = 0; i < samplesPerRow; i++)
                {
                    int dataPointIndex = (r - 1) * samplesPerRow + i;
                    if (dataPointIndex >= samples.Length) continue;

                    if (samples[dataPointIndex] > 0)
                    {
                        int valueToShow = (int)Math.Min(samples[dataPointIndex] * sampleAmplification, rowHeight);
                        int penIndex = (int)Math.Min(chartPens.Length - 1, (((samples[dataPointIndex] * sampleAmplification) / (float)valueToShow) - 1.0f) * 0.5);

                        graphics.DrawLine(chartPens[penIndex], new Point(i, bottomLine), new Point(i, bottomLine - valueToShow));
                    }
                }
            }

            int scaledDownWidth = (((int)(spectrumBitmap.Width / resoltionScaleDownFactor)) / 2) * 2;
            int scaledDownHeight = (((int)(spectrumBitmap.Height / resoltionScaleDownFactor)) / 2) * 2;
            var scaledDownBitmap = new Bitmap(spectrumBitmap, new Size(scaledDownWidth, scaledDownHeight));

            Font font = new Font("Georgia", 10.0f);
            graphics = Graphics.FromImage(scaledDownBitmap);
            graphics.DrawString($"{Path.GetFileName(filename)}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + font.Height * 0));
            graphics.DrawString($"{tags}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + font.Height * 1.1f));
            graphics.DrawString($"Y range: {Math.Round(config.RangeY * 1000 * 1000):N0} µV", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + font.Height * 2.2f));
            graphics.DrawString($"Max value: {Math.Round(maxValue * 1000 * 1000):N0} µV @ {fftData.GetBinFromIndex(maxValueAtIndex)}", font, Brushes.White, new PointF(scaledDownWidth * 0.75f, 10.0f + font.Height * 3.3f));

            int scaledDownRowHeight = (scaledDownHeight / rowCount);
            for (int r = 1; r <= rowCount; r++)
            {
                var fftBinStart = fftData.GetBinFromIndex((r - 1) * samplesPerRow);
                var fftBinEnd = fftData.GetBinFromIndex(r * samplesPerRow - 1);
                string freqStart = SimplifyNumber(fftBinStart.StartFrequency) + "Hz";
                string freqEnd = SimplifyNumber(fftBinEnd.EndFrequency) + "Hz";
                int bottomLine = r * scaledDownRowHeight;

                // string on the left side of the row
                graphics.DrawString($"{freqStart}", font, Brushes.White, new PointF(10.0f, bottomLine - (scaledDownRowHeight * 0.25f)));

                // string on the right side of the row
                graphics.DrawString($"{freqEnd}", font, Brushes.White, new PointF(scaledDownWidth - 75.0f, bottomLine - (scaledDownRowHeight * 0.25f)));
            }

            FileStream pngFile = new FileStream(filename, FileMode.Create);
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

            while ((absValue > 1) && (absValue % 1000 == 0))
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

        internal void StopDataAcquisition()
        {
            dataAcquisition.Stop();
        }

        internal static void ShowWelcomeScreen(string versionString)
        {
            Console.WriteLine($"Bio Balance Detector Body Monitor {versionString}");
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

        internal void ProcessCommandLineArguments(string[] args)
        {
            httpClient.BaseAddress = new Uri("https://localhost:7061/");

            if (args.Length > 0)
            {
                if (args[0] == "--video")
                {
                    var mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => !String.IsNullOrWhiteSpace(_config.Postprocessing.SaveAsPNG.MLProfile) && p.Name.StartsWith(_config.Postprocessing.SaveAsPNG.MLProfile));

                    if ((args.Length > 2) && (!String.IsNullOrWhiteSpace(args[2])))
                    {
                        mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(args[2]));

                        if (mlProfile == null)
                        {
                            _logger.LogError($"The profile '{args[2]}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                            return;
                        }
                    }

                    if ((args.Length > 3) && (!String.IsNullOrWhiteSpace(args[3])) && (args[3].Contains("x")))
                    {
                        int width = 0;
                        int height = 0;

                        if (Int32.TryParse(args[3].Split("x")[0], out width) && Int32.TryParse(args[3].Split("x")[1], out height))
                        {
                            _config.Postprocessing.SaveAsPNG.TargetWidth = width;
                            _config.Postprocessing.SaveAsPNG.TargetHeight = height;
                        }
                    }

                    GenerateVideo(args[1], mlProfile);
                    return;
                }

                if (args[0] == "--mlcsv")
                {
                    var httpResult = httpClient.GetAsync($"DataAcquisition/train/{args[1]}/{args[2]}/{args[3]}/{args[4]}").Result;

                    if ((httpResult != null) && (httpResult.StatusCode == System.Net.HttpStatusCode.OK))
                    {
                        Console.WriteLine("OK - TaskId: " + httpResult.Content);
                    }
                    else
                    {
                        Console.WriteLine("Error: HTTP Status Code " + httpResult.StatusCode);
                    }

                    //var mlProfile = _config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(args[2]));

                    //if (mlProfile == null)
                    //{
                    //    _logger.LogError($"The profile '{args[2]}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                    //    return;
                    //}

                    //_logger.LogInformation($"Generating ML CSV and mbconfig files based on the machine learning profile '{mlProfile.Name}'.");
                    //GenerateMLCSV(args[1], mlProfile, _config.MachineLearning.CSVHeaders, args[3], args[4]);
                    return;
                }

                if (args[0] == "--testmodels")
                {
                    if ((args.Length != 3) || (!args[2].EndsWith("%")))
                    {
                        _logger.LogInformation($"Usage:");
                        _logger.LogInformation($"bbd.bodymonitor.exe --testmodels <traning data folder> <percentage to test>");
                        return;
                    }

                    float percentageToTest = Single.Parse(args[2].Substring(0, args[2].Length - 1));

                    PreprareMachineLearningModels();
                    TestAllModels(args[1], percentageToTest);
                    return;
                }

                if (args[0] == "--calibrate")
                {
                    CalibrateDevice();
                    return;
                }

                if (args[0] == "--dataacquisition")
                {
                    var httpResult = httpClient.GetAsync("DataAcquisition/start").Result;

                    if ((httpResult != null) && (httpResult.StatusCode == System.Net.HttpStatusCode.OK))
                    {
                        Console.WriteLine("OK - TaskId: " + httpResult.Content);
                    }
                    else
                    {
                        Console.WriteLine("Error: HTTP Status Code " + httpResult.StatusCode);
                    }

                    //if (_config.Indicators.Enabled)
                    //{
                    //    PreprareMachineLearningModels();
                    //}

                    //StartDataAcquisition();
                }
            }
        }
    }
}
