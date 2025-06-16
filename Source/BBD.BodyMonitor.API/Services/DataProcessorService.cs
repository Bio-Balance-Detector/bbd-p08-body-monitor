using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Indicators;
using BBD.BodyMonitor.MLProfiles;
using BBD.BodyMonitor.Models;
using BBD.BodyMonitor.Sessions;
using EDFCSharp;
using Microsoft.Extensions.Options;
using Microsoft.ML;
using Microsoft.ML.Data;
using NWaves.Audio;
using NWaves.Signals;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xabe.FFmpeg;

namespace BBD.BodyMonitor.Services
{
    /// <summary>
    /// Provides concrete implementation for processing data, managing data acquisition from devices,
    /// and generating various outputs like CSV files for machine learning, videos, FFT data, and EDF files.
    /// This class also handles device calibration, signal generation, and indicator evaluation.
    /// It is intended for internal use and is registered as a singleton service.
    /// </summary>
    internal class DataProcessorService : IDataProcessorService
    {
        private readonly ILogger _logger;
        private BodyMonitorOptions _config;
        private readonly ISessionManagerService _sessionManager;
        private readonly float _inputAmplification = short.MaxValue / 1.0f;
        private System.Timers.Timer? _checkDiskSpaceTimer;
        private System.Timers.Timer? _signalGeneratorTimer;
        private DateTime? _signalGeneratorTimerLastCheckedDate;
        private Dictionary<string, Queue<SignalGeneratorCommand>> _signalGeneratorCommandQueues;
        private DataAcquisition? _dataAcquisition;
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
        private DateTime? _waveFileTimestamp;
        private string _currentWavFilename;
        private int _indicatorEvaluationCounter;
        private readonly List<string> disabledIndicators = new();
        private readonly ConcurrentDictionary<string, List<IndicatorEvaluationResult>> _recentIndicatorResults = new();
        private readonly Dictionary<string, Queue<string>> _waveFileWriteQueue = new();
        private readonly ConcurrentDictionary<DateTime, IndicatorEvaluationResult[]> _indicatorResultsDictionary = new();

        /// <summary>
        /// OBSOLETE constructor. Initializes a new instance of the <see cref="DataProcessorService"/> class.
        /// Dependencies are typically resolved via dependency injection in newer application patterns.
        /// </summary>
        /// <param name="logger">The logger instance for recording messages and errors.</param>
        /// <param name="configRoot">The root configuration interface, used to initialize BodyMonitorOptions.</param>
        /// <param name="bodyMonitorOptions">The options monitor for BodyMonitor configuration.</param>
        /// <param name="sessionManager">The session manager service for handling session data.</param>
        [Obsolete("This constructor is obsolete. Use dependency injection to provide necessary services and options.")]
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

        /// <summary>
        /// OBSOLETE. Prepares machine learning models, potentially by evaluating dummy data to initialize prediction engines.
        /// </summary>
        [Obsolete("This method is obsolete and its functionality may be integrated elsewhere or re-evaluated.")]
        public void PrepareMachineLearningModels()
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

        /// <summary>
        /// Lists all connected and available data acquisition devices.
        /// </summary>
        /// <returns>An array of <see cref="ConnectedDevice"/> objects detailing each detected device.</returns>
        public ConnectedDevice[] ListDevices()
        {
            return DataAcquisition.ListDevices();
        }

        /// <summary>
        /// OBSOLETE. Starts a new data acquisition session using the specified device.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device to use. If null or not found, a default device may be used.</param>
        /// <param name="session">The <see cref="Session"/> object to associate with this acquisition.</param>
        /// <returns>The serial number of the device used for acquisition, or null if starting failed.</returns>
        [Obsolete("This method is obsolete. Data acquisition start should be handled by newer mechanisms or specific controller actions.")]
        public string? StartDataAcquisition(string deviceSerialNumber, Session? session)
        {
            StartDiskSpaceChecker(spaceCheckDrive);

            // Check if a device with the serial number is available
            int deviceIndex = GetDeviceIndexFromSerialNumber(deviceSerialNumber);
            if (deviceIndex == -1)
            {
                _logger.LogWarning($"Device with serial number '{deviceSerialNumber}' not found, using default device.");
            }

            try
            {
                _dataAcquisition ??= new DataAcquisition(_logger, session);

                // Get device list
                string[] deviceNames = DataAcquisition.ListDevices().Select(d => $"#{d.Index} {d.Name} ({d.SerialNumber})").ToArray();
                string deviceList = deviceNames.Length == 0 ? "N/A" : string.Join(", ", deviceNames);
                _logger.LogInformation($"Available devices: {deviceList}.");

                if (deviceNames.Length == 0)
                {
                    return null;
                }

                // Open device
                deviceSerialNumber = _dataAcquisition.OpenDevice(deviceIndex, _config.Acquisition.Channels, _config.Acquisition.Samplerate, _config.Acquisition.Block, _config.Acquisition.Buffer);
                _dataAcquisition.ErrorHandling = BufferErrorHandlingMode.ZeroSamples;
                _dataAcquisition.BufferError += DataAcquisition_BufferError;

                if (_config.DataWriter.Enabled)
                {
                    _dataAcquisition.SubscribeToBlockReceived(_config.DataWriter.Interval, DataWriter_BlockReceived);
                }
                if (_config.Postprocessing.Enabled)
                {
                    _dataAcquisition.SubscribeToBlockReceived(_config.Postprocessing.Interval, Postprocessing_BlockReceived);
                }
                if (_config.AudioRecording.Enabled)
                {
                    _dataAcquisition.SubscribeToBlockReceived(_config.AudioRecording.Interval, AudioRecording_BlockReceived);
                }
                if (_config.Indicators.Enabled)
                {
                    _dataAcquisition.SubscribeToBlockReceived(_config.Indicators.Interval, Indicators_BlockReceived);
                }

                // Start recording on the device
                _logger.LogInformation($"Recording data on '{_dataAcquisition.SerialNumber}' at {SimplifyNumber(_dataAcquisition.Samplerate)}Hz" + (_config.Postprocessing.Enabled ? $" with the effective FFT resolution of {_dataAcquisition.Samplerate / 2.0 / (_config.Postprocessing.FFTSize / 2):0.00} Hz" : "") + "...");
                _dataAcquisition.Start();

                // Start signal generation
                StartSignalGeneration(_dataAcquisition);
            }
            catch (Exception)
            {
                return null;
            }

            return deviceSerialNumber;
        }

        private void StartDiskSpaceChecker(DriveInfo? spaceCheckDrive)
        {
            // Stop previous timer if any
            if (_checkDiskSpaceTimer != null)
            {
                _checkDiskSpaceTimer.Stop();
                _checkDiskSpaceTimer.Dispose();
                _checkDiskSpaceTimer = null;
                _logger.LogTrace("Previous timer to check disk space was stopped.");
            }
            // Check disk space every 10 seconds
            _checkDiskSpaceTimer = new(10000);
            _checkDiskSpaceTimer.Elapsed += CheckDiskSpaceTimer_Elapsed;
            _checkDiskSpaceTimer.AutoReset = true;
            _checkDiskSpaceTimer.Enabled = true;
            _logger.LogTrace($"Set up a timer to check disk space on '{spaceCheckDrive?.Name}' every {_checkDiskSpaceTimer.Interval / 1000:0} seconds.");
        }

        /// <summary>
        /// Initializes and starts the signal generation process based on configured schedules.
        /// This involves parsing schedules, creating command queues for each signal generator channel,
        /// and starting a timer to execute these commands at the appropriate times.
        /// </summary>
        /// <param name="dataAcquisitionInstance">The active <see cref="DataAcquisition"/> instance to control the signal generator on. This parameter is named `_dataAcquisition` in the original code but renamed here for clarity in documentation.</param>
        private void StartSignalGeneration(DataAcquisition? dataAcquisitionInstance)
        {
            if (dataAcquisitionInstance == null)
            {
                _logger.LogWarning("Data acquisition instance is null. Cannot start signal generation.");
                return;
            }

            TimeSpan currentTime = DateTime.UtcNow.TimeOfDay;
            currentTime = new TimeSpan(currentTime.Hours, currentTime.Minutes, currentTime.Seconds);

            _config.ParseSignalGeneratorParameters();

            // Create the signal generator command queue based on the schedule configuration
            _signalGeneratorCommandQueues = new();
            foreach (IGrouping<string, ScheduleOptions> signalGeneratorChannelGroup in _config.SignalGenerator.Schedules.GroupBy(s => s.ChannelId))
            {
                string channelId = signalGeneratorChannelGroup.Key;

                List<SignalGeneratorCommand> signalGeneratorCommands = new();
                // Create a start and stop command for each schedule
                foreach (ScheduleOptions? schedule in signalGeneratorChannelGroup.ToArray())
                {
                    // add start and stop commands
                    signalGeneratorCommands.Add(new SignalGeneratorCommand()
                    {
                        Timestamp = schedule.TimeToStart,
                        Command = SignalGeneratorCommandType.Start,
                        Options = schedule
                    });
                    signalGeneratorCommands.Add(new SignalGeneratorCommand()
                    {
                        Timestamp = schedule.TimeToStart + schedule.SignalLength,
                        Command = SignalGeneratorCommandType.Stop,
                        Options = schedule
                    });

                    // add repeating commands if the repeat period is defined
                    if (schedule.RepeatPeriod != null)
                    {
                        TimeSpan repeatUntil = schedule.TimeToStart.Add(TimeSpan.FromDays(1));
                        TimeSpan nextRepeat = schedule.TimeToStart + schedule.RepeatPeriod.Value;
                        while ((nextRepeat < repeatUntil) && ((schedule.TimeToStop == null) || (nextRepeat < schedule.TimeToStop)))
                        {
                            TimeSpan start = nextRepeat;
                            if (start.Days > 0)
                            {
                                start = start.Subtract(new TimeSpan(start.Days, 0, 0, 0));
                            }
                            signalGeneratorCommands.Add(new SignalGeneratorCommand()
                            {
                                Timestamp = start,
                                Command = SignalGeneratorCommandType.Start,
                                Options = schedule
                            });

                            TimeSpan stop = nextRepeat + schedule.SignalLength;
                            if (stop.Days > 0)
                            {
                                stop = stop.Subtract(new TimeSpan(stop.Days, 0, 0, 0));
                            }
                            signalGeneratorCommands.Add(new SignalGeneratorCommand()
                            {
                                Timestamp = stop,
                                Command = SignalGeneratorCommandType.Stop,
                                Options = schedule
                            });

                            nextRepeat += schedule.RepeatPeriod.Value;
                        }
                    }

                    // add a stop command if the time to stop is defined
                    if (schedule.TimeToStop != null)
                    {
                        signalGeneratorCommands.Add(new SignalGeneratorCommand()
                        {
                            Timestamp = schedule.TimeToStop.Value,
                            Command = SignalGeneratorCommandType.Stop,
                            Options = schedule
                        });
                    }
                }

                // Convert them all from local to UTC timezone
                foreach (SignalGeneratorCommand command in signalGeneratorCommands)
                {
                    command.Timestamp = DateTime.Today.AddTicks(command.Timestamp.Ticks).ToUniversalTime().TimeOfDay;
                }

                // Sort the list by timestamp
                signalGeneratorCommands.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

                // Rotate the list until we reach the current time
                SignalGeneratorCommand? firstCommand = signalGeneratorCommands.FirstOrDefault();
                if (firstCommand != null)
                {
                    while (signalGeneratorCommands.Count > 0 && signalGeneratorCommands[0].Timestamp < currentTime)
                    {
                        signalGeneratorCommands.Add(signalGeneratorCommands[0]);
                        signalGeneratorCommands.RemoveAt(0);

                        if (firstCommand == signalGeneratorCommands.First()) // Prevent infinite loop if all commands are in the past
                        {
                            break;
                        }
                    }
                }
                _signalGeneratorCommandQueues.Add(channelId, new Queue<SignalGeneratorCommand>(signalGeneratorCommands));
            }

            // Start the timer that handles signal generator on the device
            if (_signalGeneratorTimer != null)
            {
                _signalGeneratorTimer.Stop();
                _signalGeneratorTimer.Dispose();
            }
            _signalGeneratorTimer = new(100)
            {
                AutoReset = true,
                Enabled = true
            };
            _signalGeneratorTimer.Elapsed += _signalGeneratorTimer_Elapsed;
            _logger.LogTrace($"Set up a timer to generate signals on '{dataAcquisitionInstance.SerialNumber}'.");
        }

        /// <summary>
        /// Event handler for the signal generator timer. Checks the command queue for each channel
        /// and executes commands whose scheduled time has arrived.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="System.Timers.ElapsedEventArgs"/> that contains the event data.</param>
        private void _signalGeneratorTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            lock (_signalGeneratorCommandQueues)
            {
                DateTime currentTime = DateTime.UtcNow;
                DateTime comperasonDate = _signalGeneratorTimerLastCheckedDate.HasValue ? new(_signalGeneratorTimerLastCheckedDate.Value.Year, _signalGeneratorTimerLastCheckedDate.Value.Month, _signalGeneratorTimerLastCheckedDate.Value.Day, 0, 0, 0) : new(currentTime.Year, currentTime.Month, currentTime.Day, 0, 0, 0);

                // Execute the next command in the queue for each channel
                foreach (string channelId in _signalGeneratorCommandQueues.Keys)
                {
                    Queue<SignalGeneratorCommand> signalGeneratorCommands = _signalGeneratorCommandQueues[channelId];
                    if (signalGeneratorCommands.Count > 0)
                    {
                        SignalGeneratorCommand signalGeneratorCommand = signalGeneratorCommands.Peek();
                        DateTime timeToCheck = comperasonDate.AddMilliseconds(signalGeneratorCommand.Timestamp.TotalMilliseconds);
                        if (timeToCheck <= currentTime)
                        {
                            signalGeneratorCommand = signalGeneratorCommands.Dequeue();
                            if (signalGeneratorCommand == null) // Should not happen if Peek succeeded and Count > 0
                            {
                                break;
                            }
                            ExecuteSignalGeneratorCommand(channelId, signalGeneratorCommand);
                        }
                    }
                }
                _signalGeneratorTimerLastCheckedDate = comperasonDate;
            }
        }

        private void ExecuteSignalGeneratorCommand(string channelId, SignalGeneratorCommand signalGeneratorCommand)
        {
            if (_dataAcquisition == null)
            {
                _logger.LogWarning($"Data acquisition is not active. Cannot execute signal generator command for channel '{channelId}'.");
                return;
            }

            _logger.LogTrace($"Executing signal generator command '{signalGeneratorCommand.Command}' on channel '{channelId}' at {DateTime.Now:HH:mm:ss.fff}.");

            SignalGeneratorStatus status = _dataAcquisition.GetSignalGeneratorStatus(channelId);

            switch (signalGeneratorCommand.Command)
            {
                case SignalGeneratorCommandType.Start:
                    if (!status.IsRunning)
                    {
                        _logger.LogTrace($"Starting signal generator on channel '{channelId}'.");
                    }
                    else
                    {
                        _logger.LogTrace($"Changing signal generator parameters on channel '{channelId}'.");
                    }

                    if (signalGeneratorCommand.Options == null)
                    {
                        _logger.LogError($"Signal generator command options are null.");
                        break;
                    }

                    // Get the signal definition from the signalGeneratorCommand.Options.SignalName value
                    SignalDefinitionOptions? sd = _config.SignalGenerator.SignalDefinitions.FirstOrDefault(s => s.Name == signalGeneratorCommand.Options.SignalName);

                    if (sd == null)
                    {
                        _logger.LogError($"Signal definition '{signalGeneratorCommand.Options.SignalName}' not found.");
                        break;
                    }

                    // Pass the command paramters to the device
                    _dataAcquisition.ChangeSignalGenerator(channelId, sd.Function, sd.FrequencyFrom, sd.FrequencyFrom == sd.FrequencyTo ? null : sd.FrequencyTo, sd.FrequencyMode == PeriodicyMode.PingPong, sd.AmplitudeFrom, sd.AmplitudeFrom == sd.AmplitudeTo ? null : sd.AmplitudeTo, sd.AmplitudeMode == PeriodicyMode.PingPong, signalGeneratorCommand.Options.SignalLength);
                    break;
                case SignalGeneratorCommandType.Stop:
                    _logger.LogTrace($"Stopping signal generator on channel '{channelId}'.");
                    _dataAcquisition.StopSignalGenerator(channelId);
                    break;
            }
        }

        /// <summary>
        /// Stops an ongoing data acquisition session for the specified device.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device whose acquisition session should be stopped. If null or empty, the default device's session is targeted.</param>
        /// <returns>True if the acquisition was stopped successfully or if no acquisition was active; false if the specified device was not found or an error occurred.</returns>
        public bool StopDataAcquisition(string deviceSerialNumber)
        {
            if (_dataAcquisition == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                // get the serail number of the default device
                deviceSerialNumber = _dataAcquisition.SerialNumber;
            }

            if (_dataAcquisition.DeviceIndex != GetDeviceIndexFromSerialNumber(deviceSerialNumber))
            {
                return false;
            }

            _dataAcquisition.Stop();

            _logger.LogInformation($"Finished recording data on '{_dataAcquisition.SerialNumber}' at {SimplifyNumber(_dataAcquisition.Samplerate)}Hz" + (_config.Postprocessing.Enabled ? $" with the effective FFT resolution of {_dataAcquisition.Samplerate / 2.0 / (_config.Postprocessing.FFTSize / 2):0.00} Hz" : "") + ".");

            return true;
        }

        private void DataAcquisition_BufferError(object sender, BufferErrorEventArgs e)
        {
            _ = Task.Run(() => // Consider making this method async Task and awaiting this if it's critical path.
            {
                //_logger.LogWarning($"Data error! cAvailable: {e.BytesAvailable / (float)e.BytesTotal:P}, cLost: {e.BytesLost / (float)e.BytesTotal:P}, cCorrupted: {e.BytesCorrupted / (float)e.BytesTotal:P}");

                DataAcquisition? currentDataAcquisition = _dataAcquisition; // Capture to local variable for thread safety
                if (currentDataAcquisition == null)
                {
                    return;
                }

                if (currentDataAcquisition.ErrorHandling == BufferErrorHandlingMode.ZeroSamples)
                {
                    // The buffer is filled with zeros at the corrupted positions, so we can continue
                }

                if (currentDataAcquisition.ErrorHandling == BufferErrorHandlingMode.DiscardSamples)
                {
                    // The buffer didn't get the corrupted data, so we can continue
                }

                if (currentDataAcquisition.ErrorHandling == BufferErrorHandlingMode.ClearBuffer)
                {
                    // The buffer is going to be cleared, so we need to write the remaining data to the WAV file and start a new one

                    string oldWavFilename = _currentWavFilename;
                    _waveFileTimestamp = DateTime.UtcNow;

                    if (_waveFileWriteQueue.ContainsKey(oldWavFilename))
                    {
                        while (_waveFileWriteQueue[oldWavFilename].Count > 0)
                        {
                            Thread.Sleep(500);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Performs a calibration routine for the specified device.
        /// This involves acquiring data with known signal generator settings to measure errors.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device to calibrate.</param>
        public void CalibrateDevice(string deviceSerialNumber)
        {
            Session session = _sessionManager.StartSession(null, null);

            int deviceIndex = GetDeviceIndexFromSerialNumber(deviceSerialNumber);

            calibration = new DataAcquisition(_logger, session);
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

                _ = calibration.OpenDevice(deviceIndex, new string[] { "CH1" }, samplerate, 0.1f, 1.0f);
                calibration.ChangeSignalGenerator("W2", SignalFunction.Sine, signalFrequency, null, false, signalAmplitude, null, false, null);
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

                _ = calibration.OpenDevice(deviceIndex, new string[] { "CH1" }, samplerate, 0.1f, 1.0f);
                calibration.ChangeSignalGenerator("W2", SignalFunction.Sine, signalFrequency, null, false, signalAmplitude, null, false, null);
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

                _ = calibration.OpenDevice(deviceIndex, new string[] { "CH1" }, samplerate, 0.1f, 1.0f);
                calibration.ChangeSignalGenerator("W2", SignalFunction.Sine, signalFrequency, null, false, signalAmplitude, null, false, null);
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

        /// <summary>
        /// OBSOLETE. Performs a frequency response analysis for the specified device.
        /// This method iterates through frequency ranges, generating signals and analyzing the response.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device to analyze.</param>
        /// <exception cref="Exception">Thrown if data acquisition is not enabled in the configuration.</exception>
        [Obsolete("This method is obsolete and its functionality for frequency response analysis may be revised or removed.")]
        public void FrequencyResponseAnalysis(string deviceSerialNumber)
        {
            if (!_config.Acquisition.Enabled)
            {
                throw new Exception("Acquisition must be enabled for the frequnecy response analysis.");
            }

            Session session = _sessionManager.StartSession(null, null);

            DataAcquisition frada = new(_logger, session);
            int deviceIndex = GetDeviceIndexFromSerialNumber(deviceSerialNumber);

            FrequencyAnalysisSettings frequencyAnalysisSettings = new()
            {
                StartFrequency = 1 / _config.Acquisition.Block,
                EndFrequency = _config.Acquisition.Samplerate / 2,
                FrequencyStep = _config.Postprocessing.ResampleFFTResolutionToHz,
                Samplerate = _config.Acquisition.Samplerate,
                FftSize = _config.Postprocessing.FFTSize,
                BlockLength = _config.Acquisition.Block,
                Amplitude = 1.0f
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
                _ = frada.OpenDevice(deviceIndex, _config.Acquisition.Channels, frequencyAnalysisSettings.Samplerate, _config.Acquisition.Block, _config.Acquisition.Buffer);
                frequencyAnalysisSettings.Samplerate = frada.Samplerate; // Update with actual samplerate
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

        [Obsolete]
        private void RunPartialFrequencyAnalysis(int deviceIndex, DataAcquisition frada, FrequencyAnalysisSettings frequencyAnalysisSettings, float[] result)
        {
            fftDataBlockCache = new FftDataBlockCache((int)frequencyAnalysisSettings.Samplerate, frequencyAnalysisSettings.FftSize, frequencyAnalysisSettings.BlockLength, frequencyAnalysisSettings.FrequencyStep);

            _ = frada.OpenDevice(deviceIndex, _config.Acquisition.Channels, frequencyAnalysisSettings.Samplerate, frequencyAnalysisSettings.BlockLength, _config.Acquisition.Buffer);
            //frada.SubscribeToBlockReceived(frequencyAnalysisSettings.FftSize / frequencyAnalysisSettings.Samplerate, FrequencyResponseAnalysis_SamplesReceived);
            frada.SubscribeToBlockReceived(0.05f, FrequencyResponseAnalysis_SamplesReceived);
            // Re-initialize fftDataBlockCache after OpenDevice, as Samplerate might change
            fftDataBlockCache = new FftDataBlockCache((int)frada.Samplerate, frequencyAnalysisSettings.FftSize, frequencyAnalysisSettings.BlockLength, frequencyAnalysisSettings.FrequencyStep);


            frada.Start("CH1");

            frada.ChangeSignalGenerator("W2", SignalFunction.Sine, frequencyAnalysisSettings.StartFrequency, frequencyAnalysisSettings.EndFrequency, false, frequencyAnalysisSettings.Amplitude, null, false, TimeSpan.FromSeconds(45.0));
            //frada.ChangeSignalGeneratorFrequency(frequencyAnalysisSettings.EndFrequency);

            DateTime endTime = DateTime.UtcNow.AddSeconds(15.0); // This duration might be too short for a full sweep.
            lastMaxIndex = 0;
            //for (int i = (int)(frequencyAnalysisSettings.StartFrequency / frequencyAnalysisSettings.FrequencyStep); i < frequencyAnalysisSettings.EndFrequency / frequencyAnalysisSettings.FrequencyStep; i++)
            while (DateTime.UtcNow < endTime)
            {
                //float frequencyToTest = ((i + 0.5f) * frequencyAnalysisSettings.FrequencyStep);
                //frada.ChangeSignalGeneratorFrequency(frequencyToTest);
                //frada.ClearBuffer();
                float valueMultiplier = 1 / frequencyAnalysisSettings.Amplitude / _config.Postprocessing.FFTSize * 200000000;

                frequencyResponseAnalysisFftData = null;
                while (frequencyResponseAnalysisFftData == null) { Thread.Sleep(10); /* Wait for data */ }
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

                _logger.LogInformation($"{frada.GetSignalGeneratorStatus("W2").Frequency:0.00} Hz -> {maxValue:0.00000} @ {lastMaxIndex + maxIndex,7} ~{maxFreq,10:0.00} Hz");

                result[(int)(maxFreq / frequencyAnalysisSettings.FrequencyStep)] = maxValue;

                //lastMaxIndex += maxIndex;
            }

            frada.Stop();
            frada.CloseDevice();
        }

        /// <summary>
        /// Event handler for processing data blocks received during frequency response analysis.
        /// Computes FFT and stores results if conditions are met.
        /// </summary>
        /// <param name="sender">The source of the event (typically a <see cref="DataAcquisition"/> instance).</param>
        /// <param name="e">The <see cref="BlockReceivedEventArgs"/> containing the data block.</param>
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
                // Consider stopping or signaling an error state if this occurs.
                return;
            }
        }

        /// <summary>
        /// Event handler for the disk space check timer. Logs an error and potentially stops acquisition if free disk space is below a configured threshold.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="System.Timers.ElapsedEventArgs"/> that contains the event data.</param>
        public void CheckDiskSpaceTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (spaceCheckDrive != null && spaceCheckDrive.AvailableFreeSpace < _config.MinimumAvailableFreeSpace)
            {
                _logger.LogError($"There is not enough space on drive {spaceCheckDrive.Name}.  to continue. There must be at least {_config.MinimumAvailableFreeSpace:N0} bytes of available free space at all times.");
                _dataAcquisition?.Stop();
            }
        }

        /// <summary>
        /// Event handler for received data blocks, responsible for writing data to WAV files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="BlockReceivedEventArgs"/> containing the data block.</param>
        public void DataWriter_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            if (_dataAcquisition == null)
            {
                return;
            }

            string samplerate = $"{SimplifyNumber(_dataAcquisition.Samplerate)}sps";
            string deviceChannel = $"{_dataAcquisition.SerialNumber[^4..].ToString()?.ToLower()}-{_dataAcquisition.AcquisitionChannels[0]}";

            if (_config.DataWriter.SingleFile)
            {
                Sessions.Session session = _sessionManager.GetSession(null);
                if ((session == null) || (!session.StartedAt.HasValue))
                {
                    return;
                }

                if (_waveFileTimestamp == null)
                {
                    _waveFileTimestamp = session.StartedAt.Value.UtcDateTime;
                }

                string pathToFile = GeneratePathToFile(_waveFileTimestamp.Value);
                _currentWavFilename = $"{pathToFile}__{session.Alias}__{samplerate}__{deviceChannel}.wav";
            }
            else
            {
                _waveFileTimestamp = e.DataBlock.StartTime.ToUniversalTime().DateTime;

                string pathToFile = GeneratePathToFile(_waveFileTimestamp.Value);
                _currentWavFilename = $"{pathToFile}__{samplerate}__ch{deviceChannel}.wav";
            }
            _currentWavFilename = AppendDataDir(_currentWavFilename);

            _ = Task.Run(() =>
            {
                string threadId = e.DataBlock.StartTime.ToString("HHmmss") + "DW";
                _logger.LogTrace($"Data writer thread #{threadId} begins.");

                Stopwatch sw = Stopwatch.StartNew();

                lock (_waveFileWriteQueue)
                {
                    // create the queue if it doesn't exist
                    if (!_waveFileWriteQueue.ContainsKey(_currentWavFilename))
                    {
                        _waveFileWriteQueue.Add(_currentWavFilename, new());
                    }

                    // add the thread ID to the queue
                    _waveFileWriteQueue[_currentWavFilename].Enqueue(threadId);
                }

                // check if the thread ID is the first in the queue
                if (_waveFileWriteQueue[_currentWavFilename].Peek() != threadId)
                {
                    // if not, wait until it is
                    _logger.LogTrace($"Data writer thread #{threadId} is waiting in the queue for its turn among {_waveFileWriteQueue[_currentWavFilename].Count - 2} others.");
                    while (_waveFileWriteQueue[_currentWavFilename].Peek() != threadId)
                    {
                        Thread.Sleep(100);
                    }
                    _logger.LogTrace($"Data writer thread #{threadId} is next in the queue, {_waveFileWriteQueue[_currentWavFilename].Count - 1} others are still waiting.");
                }


                if (_config.DataWriter.SaveAsWAV)
                {
                    sw.Restart();

                    try
                    {
                        // and save samples to a WAV file
                        FileStream waveFileStream = new(_currentWavFilename, FileMode.OpenOrCreate);
                        DiscreteSignal signalToSave = new((int)_dataAcquisition.Samplerate, e.DataBlock.Data, true);
                        signalToSave.Amplify(_inputAmplification);

                        // force the values to fit into the 16-bit range
                        signalToSave = new DiscreteSignal((int)_dataAcquisition.Samplerate, signalToSave.Samples.Select(v => v < short.MinValue ? short.MinValue : v > short.MaxValue ? short.MaxValue : v).ToArray(), true);

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

                        // TODO: update the session with the WAV file name and its duration
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"#{threadId} Failed to write WAV file '{_currentWavFilename}': {ex.Message}.");
                    }

                    sw.Stop();
                    _logger.LogTrace($"#{threadId} Save as WAV completed in {sw.ElapsedMilliseconds:N0} ms.");
                }

                lock (_waveFileWriteQueue)
                {
                    _ = _waveFileWriteQueue[_currentWavFilename].Dequeue();
                }

                _logger.LogTrace($"Data writer thread #{threadId} ended.");
            }
            );
        }

        /// <summary>
        /// OBSOLETE. Event handler for processing data blocks for post-processing tasks like FFT generation and image/video creation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="BlockReceivedEventArgs"/> containing the data block.</param>
        [Obsolete("This event handler is part of an obsolete data processing flow. Consider using targeted methods like GenerateFFT or GenerateVideo directly.")]
        public void Postprocessing_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            if (_dataAcquisition == null)
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
                        if (dataBlock.Data.Max() == 0)
                        {
                            // if all samples are zero then we can't do anything
                            return;
                        }

                        resampledFFTData = fftDataBlockCache.Get(dataBlock);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        _logger.LogError($"The FFT size of {_config.Postprocessing.FFTSize:N0} is too high for the sample rate of {_config.Acquisition.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                        _dataAcquisition.Stop();
                        return;
                    }

                    if (resampledFFTData == null)
                    {
                        return;
                    }

                    sw.Stop();
                    _logger.LogTrace($"#{threadId} Signal processing completed in {sw.ElapsedMilliseconds:N0} ms.");

                    SignalGeneratorStatus signalGeneratorStatus = _dataAcquisition.GetSignalGeneratorStatus("W2");

                    if (signalGeneratorStatus.IsRunning)
                    {
                        int fftDataIndex = (int)((signalGeneratorStatus.Frequency - resampledFFTData.FirstFrequency) / resampledFFTData.FrequencyStep);
                        int fftDataIndexStart = fftDataIndex - 2;
                        int fftDataIndexEnd = fftDataIndex + 2 + 1;

                        if (fftDataIndex - 1 < 0 || fftDataIndex + 1 > resampledFFTData.MagnitudeData.Length - 1)
                        {
                            _logger.LogWarning($"#{threadId} The signal generator generates a signal at {signalGeneratorStatus.Frequency:N} Hz and that is out of the range of the current sampling ({resampledFFTData.FirstFrequency:N} Hz - {resampledFFTData.LastFrequency:N} Hz).");
                        }
                        else
                        {
                            _logger.LogInformation($"#{threadId} Normalized magnitude values around {signalGeneratorStatus.Frequency} Hz are ({string.Join(" | ", resampledFFTData.MagnitudeData[fftDataIndexStart..fftDataIndexEnd].Select(m => string.Format("{0,7:N}", m * 1000 * 1000)))}) µV.");
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

        /// <summary>
        /// OBSOLETE. Event handler for processing data blocks to evaluate bio-indicators using machine learning models.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="BlockReceivedEventArgs"/> containing the data block.</param>
        [Obsolete("This event handler is part of an obsolete data processing flow. Indicator evaluation should be handled by newer mechanisms.")]
        public void Indicators_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            if (_dataAcquisition == null)
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
                        _logger.LogError($"#{threadId} The FFT size of {_config.Postprocessing.FFTSize:N0} is too high for the sample rate of {_config.Acquisition.Samplerate:N0}. Decrease the FFT size or increase the sampling rate.");
                        _dataAcquisition.Stop();
                        return;
                    }

                    if (resampledFFTData == null)
                    {
                        _logger.LogWarning($"#{threadId} No FFT data is available.");
                        return;
                    }

                    if (_config.Indicators.Enabled)
                    {
                        IndicatorEvaluationResult[] indicatorResults = EvaluateIndicators(_logger, dataBlock.EndIndex, resampledFFTData);
                        _ = _indicatorResultsDictionary.TryAdd(DateTime.UtcNow, indicatorResults);

                        if (indicatorResults.Length > 0)
                        {
                            _logger.LogInformation($"#{threadId} {resampledFFTData.Start:HH:mm:ss}-{resampledFFTData.Start?.AddSeconds(_config.Postprocessing.Interval):HH:mm:ss} {string.Join(" | ", indicatorResults.Select(er => er.Text + " " + (!er.Negate ? er.PredictionScore : 1.0f - er.PredictionScore).ToString("+0.00;-0.00; 0.00").PadLeft(7)))}.");
                        }
                    }

                    _logger.LogTrace($"Indicators thread #{threadId} end");
                }
                );
            }
        }

        /// <summary>
        /// Event handler for processing data blocks for audio recording.
        /// Captures audio using FFmpeg based on configuration and detected signal levels.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="BlockReceivedEventArgs"/> containing the data block (not directly used for audio capture time reference in this impl, but indicates a data processing tick).</param>
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

        /// <summary>
        /// Enumerates available FFmpeg audio input devices on the system.
        /// It tries different audio frameworks (dshow, alsa, etc.) to discover devices.
        /// </summary>
        /// <returns>A list of strings, where each string represents an audio device in the format "framework/Device Name".</returns>
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

        /// <summary>
        /// OBSOLETE. Evaluates a predefined set of bio-indicators based on the input FFT data.
        /// This method iterates through a hardcoded list of <see cref="IndicatorEvaluationTaskDescriptor"/>s.
        /// </summary>
        /// <param name="logger">A logger instance. Can be null, in which case internal logging is used.</param>
        /// <param name="blockIndex">The index of the data block being evaluated.</param>
        /// <param name="inputData">The <see cref="FftDataV3"/> to use as input for the models.</param>
        /// <returns>An array of <see cref="IndicatorEvaluationResult"/> containing the results for each evaluated indicator.</returns>
        [Obsolete("This method uses a hardcoded set of indicators and models. A more dynamic approach is recommended.")]
        public IndicatorEvaluationResult[] EvaluateIndicators(ILogger? logger, long blockIndex, FftDataV3 inputData)
        {
            List<IndicatorEvaluationResult> result = new();
            List<IndicatorEvaluationResult> mostRecentResults = new();

            IndicatorEvaluationTaskDescriptor[] taskDescriptors = new IndicatorEvaluationTaskDescriptor[]
            {
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 0,
                    IndicatorName = "IsSubject_None",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20230907__TrainingData__MLP14_0p25Hz-6250Hz__IsSubject_None__2540rows__#005_0,9901.zip",
                    Negate = true,
                    Text = "Attached?",
                    Description = "Is the device attached to a subject?"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 1,
                    IndicatorName = "IsSubject_0xBAC08836",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20230907__TrainingData__MLP14_0p25Hz-6250Hz__IsSubject_0xBAC08836__3500rows__#000_0,9906.zip",
                    Negate = false,
                    Text = "Andras?",
                    Description = "Is the subject Andras?"
                },
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 2,
                //    IndicatorName = "IsSubject_0x81D21088",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                //    MLModelFilename = "BBD_20230907__TrainingData__MLP14_0p25Hz-6250Hz__IsSubject_0x81D21088__3500rows__#007_0,9812.zip",
                //    Negate = false,
                //    Text = "Water?",
                //    Description = "Is the subject water?"
                //},
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 3,
                    IndicatorName = "IsAdditive_HimalayanSalt",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20230907__TrainingData__MLP14_0p25Hz-6250Hz__IsSubject_None__2540rows__#000_0,9800.zip",
                    Negate = false,
                    Text = "High LDL Cholesterol?",
                    Description = "Do they have high LDL cholesterol levels?"
                },
                new IndicatorEvaluationTaskDescriptor()
                {
                    BlockIndex = blockIndex,
                    IndicatorIndex = 4,
                    IndicatorName = "IsAdditive_20pcVinegar",
                    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                    MLModelFilename = "BBD_20230907__TrainingData__MLP14_0p25Hz-6250Hz__IsAdditive_20pcVinegar__3500rows__#007_0,9721.zip",
                    Negate = true,
                    Text = "E. coli?",
                    Description = "Do they have Escherichia coli bacteria?"
                },
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 1,
                //    IndicatorName = "IsActivity_WorkingAtComputer",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                //    MLModelFilename = "BBD_20230824__TrainingData__MLP14_0p25Hz-6250Hz__IsActivity_WorkingAtComputer__460rows__#000_1,0000.zip",
                //    Negate = false,
                //    Text = "Working?",
                //    Description = "Is the subject working at a computer?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 2,
                //    IndicatorName = "IsActivity_Meditation",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                //    MLModelFilename = "BBD_20230824__TrainingData__MLP14_0p25Hz-6250Hz__IsActivity_Meditation__618rows__#000_1,0000.zip",
                //    Negate = false,
                //    Text = "Meditating?",
                //    Description = "Is the subject meditating?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 3,
                //    IndicatorName = "IsActivity_DoingPushups",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                //    MLModelFilename = "BBD_20230824__TrainingData__MLP14_0p25Hz-6250Hz__IsActivity_DoingPushups__158rows__#000_1,0000.zip",
                //    Negate = false,
                //    Text = "Doing pushups?",
                //    Description = "Is the subject doing pushups?"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 2,
                //    IndicatorName = "Session_SegmentedData_Sleep_Level",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP15")),
                //    MLModelFilename = "BBD_20221106__TrainingData.Sleep.MLP15__MLP15_1p00Hz-25000Hz__Session_SegmentedData_Sleep_Level__15372rows__#08_0,5312.zip",
                //    DisplayText = "Sleep stage (MLP15-R2):"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 3,
                //    IndicatorName = "Session_SegmentedData_Sleep_Level",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP16")),
                //    MLModelFilename = "BBD_20221106__TrainingData.Sleep.MLP16__MLP16_5p00Hz-125000Hz__Session_SegmentedData_Sleep_Level__15372rows__#06_0,6588.zip",
                //    DisplayText = "Sleep stage (MLP16-R2):"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 5,
                //    IndicatorName = "Session_SegmentedData_BloodTest_Cholesterol",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                //    MLModelFilename = "BBD_20221028__TrainingData.BloodTest__MLP14_0p25Hz-6250Hz__Session_SegmentedData_BloodTest_Cholesterol__560rows__#00_0,9943.zip",
                //    DisplayText = "Cholesterol (MLP14-R1):"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 6,
                //    IndicatorName = "Session_SegmentedData_HeartRate_BeatsPerMinute",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP12")),
                //    MLModelFilename = "BBD_20221122__TrainingData.HeartRate.MLP12__MLP12_0p25Hz-250Hz__Session_SegmentedData_HeartRate_BeatsPerMinute__10613rows__#42_0,5182.zip",
                //    DisplayText = "Heart BPM (MLP12-R1):"
                //},
                //new IndicatorEvaluationTaskDescriptor()
                //{
                //    BlockIndex = blockIndex,
                //    IndicatorIndex = 7,
                //    IndicatorName = "Session_SegmentedData_HeartRate_BeatsPerMinute",
                //    MLProfile = _config.MachineLearning.Profiles.First(p => p.Name.StartsWith("MLP14")),
                //    MLModelFilename = "BBD_20221122__TrainingData.HeartRate.MLP14__MLP14_0p25Hz-6250Hz__Session_SegmentedData_HeartRate_BeatsPerMinute__10613rows__#17_0,4278.zip",
                //    DisplayText = "Heart BPM (MLP14-R1):"
                //},
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
                if (!_config.Indicators.ModelsToUse.Contains(td.IndicatorName))
                {
                    continue;
                }

                Task task = Task.Run(() =>
                {
                    IndicatorEvaluationResult evalResult = EvaluateIndicator(blockIndex, td.IndicatorIndex, td.IndicatorName, td.Negate, td.Text, td.Description, td.MLModelFilename, inputData.ApplyMLProfile(td.MLProfile));
                    if (evalResult != null)
                    {
                        lock (mostRecentResults)
                        {
                            mostRecentResults.Add(evalResult);
                        }
                    }
                });
                tasks.Add(task);
            }
            Task.WaitAll(tasks.ToArray());

            sw.Stop();
            logger?.LogTrace($"#{blockIndex:0000} Predictions were completed in {sw.ElapsedMilliseconds:N0} ms.");

            _indicatorEvaluationCounter++;
            foreach (IndicatorEvaluationResult? ir in mostRecentResults.OrderBy(r => r.IndicatorIndex))
            {
                string indicatorKey = $"{ir.IndicatorIndex:0000}_{ir.IndicatorName}";

                if (!_recentIndicatorResults.ContainsKey(indicatorKey))
                {
                    _ = _recentIndicatorResults.TryAdd(indicatorKey, new List<IndicatorEvaluationResult>());
                }

                if (_config.Indicators.AverageOf > _recentIndicatorResults[indicatorKey].Count)
                {
                    _recentIndicatorResults[indicatorKey].Add(ir);
                }
                else
                {
                    _recentIndicatorResults[indicatorKey][_indicatorEvaluationCounter % _config.Indicators.AverageOf] = ir;
                }
            }

            foreach (List<IndicatorEvaluationResult> recentIndicatorResult in _recentIndicatorResults.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
            {
                IndicatorEvaluationResult? firstResult = recentIndicatorResult?.FirstOrDefault();

                if (firstResult == null)
                {
                    continue;
                }

                result.Add(new IndicatorEvaluationResult()
                {
                    BlockIndex = firstResult.BlockIndex,
                    IndicatorIndex = firstResult.IndicatorIndex,
                    IndicatorName = firstResult.IndicatorName,
                    Negate = firstResult.Negate,
                    Text = firstResult.Text,
                    Description = firstResult.Description,
                    PredictionScore = recentIndicatorResult.Average(r => r.PredictionScore),
                    Value = recentIndicatorResult.Average(r => r.Value)
                });
            }

            return result.OrderBy(r => r.IndicatorIndex).ToArray();
        }

        /// <summary>
        /// Evaluates a single bio-indicator using a specified machine learning model and input FFT data.
        /// </summary>
        /// <param name="blockIndex">The index of the data block.</param>
        /// <param name="indicatorIndex">An index for the indicator.</param>
        /// <param name="indicatorName">The name of the indicator.</param>
        /// <param name="negate">Whether to negate the prediction score.</param>
        /// <param name="text">Display text for the indicator.</param>
        /// <param name="description">Description of the indicator.</param>
        /// <param name="mlModelFilename">Filename of the ML.NET model to use.</param>
        /// <param name="inputData">The <see cref="FftDataV3"/> processed according to the model's expected profile.</param>
        /// <returns>An <see cref="IndicatorEvaluationResult"/>, or null if the model file is not found.</returns>
        /// <exception cref="Exception">Thrown if the input data's ML profile is not set or not supported.</exception>
        public IndicatorEvaluationResult? EvaluateIndicator(long blockIndex, int indicatorIndex, string indicatorName, bool negate, string text, string description, string mlModelFilename, FftDataV3 inputData)
        {
            if (!System.IO.File.Exists(mlModelFilename))
            {
                mlModelFilename = Path.Combine(GetConfig().DataDirectory, Path.GetFileName(mlModelFilename));
            }

            if (!System.IO.File.Exists(mlModelFilename))
            {
                _logger.LogWarning($"The ML model file '{mlModelFilename}' does not exist.");
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
                Negate = negate,
                Text = text,
                Description = description,
                Value = (float)metric.MeanAbsoluteError,
                PredictionScore = (float)metric.MeanAbsoluteError
            };
        }

        /// <summary>
        /// Generates a relative path string for a file based on the capture time, typically in "yyyy-MM-dd/EMR_yyyyMMdd_HHmmss" format.
        /// Ensures the date-based subdirectory exists.
        /// </summary>
        /// <param name="captureTime">The timestamp used to generate the path and filename components.</param>
        /// <returns>A string representing the relative path for the file.</returns>
        public string GeneratePathToFile(DateTime captureTime)
        {
            if (!Directory.Exists(AppendDataDir(captureTime.ToString("yyyy-MM-dd"))))
            {
                _ = Directory.CreateDirectory(AppendDataDir(captureTime.ToString("yyyy-MM-dd")));
            }

            string foldername = $"{captureTime:yyyy-MM-dd}";
            string filename = $"EMR_{captureTime:yyyyMMdd_HHmmss}";
            return Path.Combine(foldername, filename);
        }

        /// <summary>
        /// Generates a filename for a binary FFT data file (.bfft).
        /// The filename includes the start time, FFT range or ML profile name, and an optional frame counter.
        /// </summary>
        /// <param name="fftData">The <see cref="FftDataV3"/> object containing metadata for the filename.</param>
        /// <param name="frameCounter">Optional. A frame counter to include in the filename.</param>
        /// <param name="frameCounterFormatterString">The format string for the frame counter (e.g., "D4").</param>
        /// <returns>A string representing the generated filename for the binary FFT data.</returns>
        public string GenerateBinaryFftFilename(FftDataV3 fftData, int? frameCounter, string frameCounterFormatterString)
        {
            string fftRangeString = $"__{fftData.GetFFTRange()}";
            string mlProfileString = $"__{fftData.MLProfileName.Split("_")[0]}";
            string profileOrRangeString = string.IsNullOrEmpty(mlProfileString) ? fftRangeString : mlProfileString;
            string frameCounterString = frameCounter.HasValue ? $"__fr{frameCounter.Value.ToString(frameCounterFormatterString)}" : "";
            return $"EMR_{fftData.Start.Value:yyyyMMdd_HHmmss}{profileOrRangeString}{frameCounterString}.bfft";
        }

        /// <summary>
        /// OBSOLETE. Saves an FFT data signal as a PNG image with specified formatting and annotations.
        /// </summary>
        /// <param name="filename">The full path and filename for the output PNG image.</param>
        /// <param name="fftData">The <see cref="FftDataV3"/> object to render.</param>
        /// <param name="config">Configuration options for saving the PNG, such as target dimensions and value ranges.</param>
        /// <param name="mlProfile">The <see cref="MLProfile"/> used to process/filter the FFT data before rendering.</param>
        /// <param name="notes">Optional. Additional notes or text to render onto the image.</param>
        /// <exception cref="Exception">Thrown if the calculated image dimensions are too large.</exception>
        [Obsolete("This method is obsolete. Image generation should be handled by a more flexible and robust visualization component if needed.")]
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

        /// <summary>
        /// Simplifies a numeric value by appending a metric prefix (e.g., k, M) or scaling to milli (m), micro (u), etc.
        /// </summary>
        /// <param name="n">The number to simplify.</param>
        /// <param name="format">The number format string to apply after scaling.</param>
        /// <returns>A string representation of the simplified number with its metric prefix.</returns>
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

        /// <summary>
        /// Displays a welcome screen to the console with application version and command-line help.
        /// </summary>
        /// <param name="versionString">The version string of the application to display.</param>
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

        /// <summary>
        /// OBSOLETE. Processes command line arguments to invoke specific functionalities like video generation, model testing, or calibration.
        /// </summary>
        /// <param name="args">The command line arguments passed to the application.</param>
        [Obsolete("This method for argument parsing is obsolete. Use a dedicated command-line parsing library or ASP.NET Core's configuration system.")]
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

                    PrepareMachineLearningModels();
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
                        PrepareMachineLearningModels();
                    }

                    _ = StartDataAcquisition(deviceSerialNumber, null);
                }
            }
        }

        /// <summary>
        /// Gets the full path for a machine learning model file, assuming it's located in an "MLModels" subdirectory relative to the application.
        /// </summary>
        /// <param name="filename">The name of the model file.</param>
        /// <returns>The full absolute path to the model file.</returns>
        internal static string GetFullMLPath(string filename)
        {
            return Path.GetFullPath(@"MLModels\" + filename);
        }

        /// <summary>
        /// Retrieves the current application configuration.
        /// </summary>
        /// <returns>The current <see cref="BodyMonitorOptions"/>.</returns>
        public BodyMonitorOptions GetConfig()
        {
            return _config;
        }

        /// <summary>
        /// Sets the application configuration.
        /// </summary>
        /// <param name="config">The <see cref="BodyMonitorOptions"/> to apply.</param>
        public void SetConfig(BodyMonitorOptions config)
        {
            _config = config;
        }

        /// <summary>
        /// Generates Fast Fourier Transform (FFT) data from WAV files in a specified folder.
        /// It processes each WAV file, segmenting it based on <paramref name="interval"/> and <paramref name="dataBlockLength"/> (from config),
        /// applies the given <paramref name="mlProfile"/>, and saves the resulting FFT data as binary (.bfft) files.
        /// </summary>
        /// <param name="foldername">The root folder containing WAV files to process. Subdirectories are also searched.</param>
        /// <param name="mlProfile">The <see cref="MLProfile"/> to apply to the FFT data.</param>
        /// <param name="interval">The interval in seconds at which to create FFT data blocks from the WAV files.</param>
        /// <exception cref="Exception">Thrown if there is an error during file processing or FFT generation.</exception>
        public void GenerateFFT(string foldername, MLProfile mlProfile, float interval)
        {
            float dataBlockLength = _config.Postprocessing.DataBlock;

            string folderFullPath = AppendDataDir(foldername);

            if (Directory.Exists(folderFullPath))
            {
                // calculate the total size of the files to process
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

                // start a thread that shows the progress
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

                // generate the FFT files
                foreach (string filename in Directory.GetFiles(folderFullPath, "*.wav", SearchOption.AllDirectories).OrderBy(n => n))
                {
                    string wavFilename = Path.GetFullPath(filename);

                    try
                    {
                        FileInfo fi = new(wavFilename);

                        FileStream waveFileStream = new(wavFilename, FileMode.Open);
                        WavePcmFormatHeader waveHeader = WaveFileExtensions.ReadWaveFileHeader(waveFileStream);

                        FftDataBlockCache fftDataBlockCacheTemp = new((int)waveHeader.SampleRate, _config.Postprocessing.FFTSize, dataBlockLength, _config.Postprocessing.ResampleFFTResolutionToHz);

                        // calculate the estimated start time of the file
                        float waveFileTotalLength = (float)waveHeader.DataChunkSize / (waveHeader.SampleRate * waveHeader.BytesPerSample);
                        DateTime lastWriteTime = File.GetLastWriteTimeUtc(wavFilename);
                        DateTime estimatedStartTime = lastWriteTime.AddSeconds(-waveFileTotalLength);

                        // calculate the number of frames
                        int frameCounter = 0;
                        int totalFrameCount = (int)((waveFileTotalLength - dataBlockLength) / interval) + 1;
                        string frameCounterFormatterString = new('0', totalFrameCount.ToString().Length);

                        sw.Start();
                        for (float position = 0.0f; position + dataBlockLength < waveFileTotalLength; position += interval)
                        {
                            frameCounter++;
                            totalBytesProcessed += fi.Length / totalFrameCount;

                            DateTime currentTime = estimatedStartTime.AddSeconds(position);

                            try
                            {
                                // read the data block from the WAV file
                                DiscreteSignal ds = WaveFileExtensions.ReadAsDiscreteSignal(waveFileStream, position, dataBlockLength);

                                // generate the FFT data
                                FftDataV3 fftData = fftDataBlockCacheTemp.CreateFftData(ds, currentTime, (int)(waveHeader.SampleRate * dataBlockLength));

                                // apply the machine learning profile
                                fftData = fftData.ApplyMLProfile(mlProfile);

                                // save the FFT data to a binary file
                                string pathToFile = Path.Combine(Path.GetDirectoryName(wavFilename), GenerateBinaryFftFilename(fftData, frameCounter, frameCounterFormatterString));
                                FftDataV3.SaveAsBinary(fftData, pathToFile, false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"Error while generating FFT file for {wavFilename} at {currentTime}: {ex.Message}");
                                totalBytesToProcess = -1;
                            }
                        }
                        sw.Stop();
                        //_logger.LogInformation($"Generated all the FFT files for '{filename}'.");
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"There was an error while generating FFT files for {wavFilename}: {ex.Message}");
                    }
                }
                totalBytesProcessed = totalBytesToProcess;
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

        /// <summary>
        /// Gets the device index for a given device serial number by querying available devices.
        /// </summary>
        /// <param name="deviceSerialNumber">The serial number of the device.</param>
        /// <returns>The zero-based index of the device if found; otherwise, -1.</returns>
        private int GetDeviceIndexFromSerialNumber(string deviceSerialNumber)
        {
            ConnectedDevice? device = ListDevices().FirstOrDefault(d => d.SerialNumber.ToLowerInvariant() == deviceSerialNumber?.ToLowerInvariant());

            return device != null ? device.Index : -1;
        }

        /// <summary>
        /// Retrieves the most recently computed indicator evaluation results.
        /// </summary>
        /// <returns>An array of <see cref="IndicatorEvaluationResult"/> from the latest evaluation, or null if no results are available.</returns>
        public IndicatorEvaluationResult[]? GetLatestIndicatorResults()
        {
            DateTime lastKey = _indicatorResultsDictionary.OrderBy(ir => ir.Key).LastOrDefault().Key;

            _ = _indicatorResultsDictionary.TryGetValue(lastKey, out IndicatorEvaluationResult[]? result);

            return result;
        }
    }
}
