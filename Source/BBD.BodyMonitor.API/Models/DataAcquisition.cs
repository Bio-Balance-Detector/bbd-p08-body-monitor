using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Sessions;
using static BBD.BodyMonitor.Buffering.ShiftingBuffer;

namespace BBD.BodyMonitor.Models
{
    /// <summary>
    /// Handles the data acquisition process from a Digilent WaveForms (DWF) device.
    /// This class is internal and primarily used by services within the API.
    /// </summary>
    internal class DataAcquisition
    {
        /// <summary>
        /// Gets the index of the Digilent WaveForms device being used.
        /// </summary>
        public int DeviceIndex { get; internal set; }
        /// <summary>
        /// Gets the serial number of the connected device.
        /// </summary>
        public string SerialNumber { get; private set; }
        /// <summary>
        /// Gets the actual sampling rate of the acquisition in Hz.
        /// </summary>
        public float Samplerate { get; private set; }
        /// <summary>
        /// Gets the total number of samples in the acquisition buffer. This defines the capacity of the internal <see cref="ShiftingBuffer"/>.
        /// </summary>
        public int BufferSize { get; internal set; }
        /// <summary>
        /// Gets the number of samples per block. Data is processed in blocks of this size.
        /// </summary>
        public int BlockSize { get; internal set; }
        /// <summary>
        /// Gets the length of each data block in seconds.
        /// </summary>
        public float BlockLength { get; private set; }
        /// <summary>
        /// Gets the total length of the acquisition buffer in seconds.
        /// </summary>
        public float BufferLength { get; private set; }
        /// <summary>
        /// Gets the array of acquisition channel identifiers (e.g., "CH1", "CH2").
        /// </summary>
        public string[] AcquisitionChannels { get; private set; }
        private BufferErrorHandlingMode _bufferErrorHandling;
        /// <summary>
        /// Gets or sets the error handling mode for the internal samples buffer.
        /// </summary>
        public BufferErrorHandlingMode ErrorHandling
        {
            get
            {
                if (samplesBuffer != null)
                {
                    _bufferErrorHandling = samplesBuffer.ErrorHandlingMode;
                }

                return _bufferErrorHandling;
            }

            set
            {
                _bufferErrorHandling = value;

                if (samplesBuffer != null)
                {
                    samplesBuffer.ErrorHandlingMode = value;
                }
            }
        }

        private readonly ILogger _logger;
        private readonly Session _session;
        private readonly int openDeviceMaxRetryCount = 5;
        private int dwfHandle = -1;

        /// <summary>
        /// Number of samples per buffer on the device itself, not the ShiftingBuffer.
        /// </summary>
        private int bufferSize;

        private ShiftingBuffer samplesBuffer;
        /// <summary>
        /// Occurs when an error is detected in the data buffer, such as data loss or corruption.
        /// </summary>
        public event BufferErrorEventHandler BufferError;
        private double[] voltData;

        private bool terminateAcquisition = false;

        private readonly Dictionary<int, List<Action<object, BlockReceivedEventArgs>>> subscribers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAcquisition"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="session">The session object associated with this data acquisition instance.</param>
        public DataAcquisition(ILogger logger, Session session)
        {
            _logger = logger;
            _session = session;

            logger.LogInformation($"DWF Version: {GetDwfVersion()}");
        }

        /// <summary>
        /// Gets the version of the DWF library currently in use.
        /// </summary>
        /// <returns>A string representing the DWF library version, or "N/A" if it cannot be retrieved.</returns>
        public static string? GetDwfVersion()
        {
            try
            {
                _ = dwf.FDwfGetVersion(out string dwfVersion);
                return dwfVersion;
            }
            catch (Exception)
            {
                return "N/A";
            }
        }

        /// <summary>
        /// Lists all connected Digilent devices.
        /// </summary>
        /// <returns>An array of <see cref="ConnectedDevice"/> objects, each representing a detected device.</returns>
        public static ConnectedDevice[] ListDevices()
        {
            List<ConnectedDevice> result = new();

            string? dwfVersion = GetDwfVersion();
            if (dwfVersion != null)
            {
                _ = dwf.FDwfEnum(dwf.enumfilterAll, out int deviceCount);
                for (int idxDevice = 0; idxDevice < deviceCount; idxDevice++)
                {
                    _ = dwf.FDwfEnumDeviceType(idxDevice, out int deviceId, out int deviceRevision);
                    _ = dwf.FDwfEnumDeviceIsOpened(idxDevice, out int isOpened);
                    _ = dwf.FDwfEnumUserName(idxDevice, out string userName);
                    _ = dwf.FDwfEnumDeviceName(idxDevice, out string deviceName);
                    _ = dwf.FDwfEnumSN(idxDevice, out string serialNumber);

                    ConnectedDevice device = new()
                    {
                        Brand = "Digilent",
                        Library = "DWF v" + dwfVersion,
                        Index = idxDevice,
                        Id = deviceId,
                        Revision = deviceRevision,
                        IsOpened = isOpened == 1,
                        UserName = userName,
                        Name = deviceName,
                        SerialNumber = serialNumber
                    };

                    result.Add(device);
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Opens the specified Digilent device and configures it for data acquisition.
        /// </summary>
        /// <param name="deviceIndex">The index of the device to open. Use -1 to attempt to open the first available device.</param>
        /// <param name="acquisitionChannels">An array of channel identifiers (e.g., "CH1", "CH2") to be used for acquisition.</param>
        /// <param name="acquisitionSamplerate">The desired sampling rate in Hz.</param>
        /// <param name="blockLength">The desired length of each data block in seconds.</param>
        /// <param name="bufferLength">The desired total length of the acquisition buffer in seconds. Must be a multiple of <paramref name="blockLength"/>.</param>
        /// <returns>The serial number of the opened device if successful; otherwise, null.</returns>
        /// <remarks>This method includes a retry mechanism if opening the device fails initially.</remarks>
        /// <exception cref="Exception">Thrown if <paramref name="bufferLength"/> is not a multiple of <paramref name="blockLength"/>.</exception>
        public string? OpenDevice(int deviceIndex, string[] acquisitionChannels, float acquisitionSamplerate, float blockLength, float bufferLength)
        {
            DeviceIndex = deviceIndex;
            AcquisitionChannels = acquisitionChannels;
            Samplerate = acquisitionSamplerate;
            BlockLength = blockLength;
            BufferLength = bufferLength;

            if (deviceIndex == -1)
            {
                // get the default device
                _ = dwf.FDwfEnum(dwf.enumfilterAll, out int deviceCount);
                if (deviceCount == 0)
                {
                    _logger.LogError("No device found.");
                    return null;
                }

                deviceIndex = 0;
            }

            // get the serial number of the device
            _ = dwf.FDwfEnumSN(deviceIndex, out string serialNumber);
            SerialNumber = serialNumber;

            // Open the AD2 with the 2nd configuration with 16k analog-in buffer
            _ = dwf.FDwfDeviceConfigOpen(deviceIndex, 1, out dwfHandle);

            int openDeviceRetryCount = 0;
            while (dwfHandle == dwf.hdwfNone && !terminateAcquisition && (openDeviceRetryCount < openDeviceMaxRetryCount))
            {
                _ = dwf.FDwfGetLastErrorMsg(out string lastError);
                _logger.LogWarning($"Failed to open device '{SerialNumber}': {lastError.ReplaceLineEndings(" ").TrimEnd()}. Retrying in 10 seconds.");

                Thread.Sleep(10000);

                _ = dwf.FDwfDeviceConfigOpen(deviceIndex, 1, out dwfHandle);
                openDeviceRetryCount++;
            }

            if (openDeviceRetryCount == openDeviceMaxRetryCount)
            {
                return null;
            }

            _ = dwf.FDwfAnalogInBufferSizeInfo(dwfHandle, out int bufferSizeMinimum, out int bufferSizeMaximum);
            bufferSize = Math.Min(bufferSizeMaximum, (int)acquisitionSamplerate); // this refers to the device's internal buffer, not our ShiftingBuffer
            _logger.LogTrace($"Device buffer size range: {bufferSizeMinimum:N0} - {bufferSizeMaximum:N0} samples, set to {bufferSize:N0}.");
            voltData = new double[bufferSize];

            // set up acquisition parameters
            _ = dwf.FDwfAnalogInFrequencySet(dwfHandle, acquisitionSamplerate);
            _ = dwf.FDwfAnalogInFrequencyGet(dwfHandle, out double realSamplerate);

            Samplerate = (float)realSamplerate;
            if (acquisitionSamplerate != (int)realSamplerate)
            {
                _logger.LogWarning($"The sampling rate of {acquisitionSamplerate:N} Hz is not supported, so the effective sampling rate was set to {realSamplerate:N} Hz. Native sampling rates for AD2 are the following: 1, 2, 4, 5, 8, 10, 16, 20, 25, 32, 40, 50, 64, 80, 100, 125, 128, 160, 200, 250, 256, 320, 400, 500, 625, 640, 800 Hz, 1, 1.25, 1.28, 1.6, 2, 2.5, 3.125, 3.2, 4, 5, 6.25, 6.4, 8, 10, 12.5, 15.625, 16, 20, 25, 31.250, 32, 40, 50, 62.5, 78.125, 80, 100, 125, 156.25, 160, 200, 250, 312.5, 390.625, 400, 500, 625, 781.25, 800 kHz, 1, 1.25, 1.5625, 2, 2.5, 3.125, 4, 5, 6.25, 10, 12.5, 20, 25, 50 and 100 MHz.");
            }

            if ((bufferLength / blockLength) - Math.Truncate(bufferLength / blockLength) != 0)
            {
                throw new Exception($"Buffer length {bufferLength}s and block length {blockLength}s must result in an integer number of blocks. Adjust your settings!");
            }
            int blockCountInBuffer = (int)(bufferLength / blockLength);

            BlockSize = (int)Math.Floor(Samplerate * blockLength);
            BufferSize = blockCountInBuffer * BlockSize; // This is the ShiftingBuffer size
            subscribers.Clear();

            _ = dwf.FDwfAnalogInBufferSizeSet(dwfHandle, bufferSize); // Set device buffer size
            _ = dwf.FDwfAnalogInAcquisitionModeSet(dwfHandle, dwf.acqmodeRecord);
            _ = dwf.FDwfAnalogInRecordLengthSet(dwfHandle, -1); // Set for continuous record

            foreach (string channelId in acquisitionChannels)
            {
                byte acquisitionChannelIndex = GetDataAcquisitionChannelIndex(channelId);

                _ = dwf.FDwfAnalogInChannelEnableSet(dwfHandle, acquisitionChannelIndex, 1);
                _ = dwf.FDwfAnalogInChannelRangeSet(dwfHandle, acquisitionChannelIndex, 5.0); // Default range
            }

            // wait at least 2 seconds for the offset to stabilize
            Thread.Sleep(2000);

            // start data acquisition on all channels
            _ = dwf.FDwfAnalogInConfigure(dwfHandle, 0, 1);

            return serialNumber;
        }

        /// <summary>
        /// Closes the connection to the currently open Digilent device.
        /// </summary>
        public void CloseDevice()
        {
            _ = dwf.FDwfDeviceClose(dwfHandle);
        }

        /// <summary>
        /// Resets the currently open Digilent device by closing and reopening it with the existing settings.
        /// </summary>
        /// <returns>The serial number of the re-opened device if successful; otherwise, null.</returns>
        public string? ResetDevice()
        {
            try
            {
                CloseDevice();
            }
            catch { }

            return OpenDevice(DeviceIndex, AcquisitionChannels, Samplerate, BlockLength, BufferLength);
        }

        /// <summary>
        /// Starts the data acquisition loop on a separate thread.
        /// </summary>
        /// <param name="channelId">The primary channel identifier (e.g., "CH1") from which to acquire data. This determines the channel index used in <c>FDwfAnalogInStatusData</c>.</param>
        /// <remarks>
        /// The acquisition loop runs on a dedicated high-priority thread.
        /// It periodically logs error summaries if data loss or corruption occurs.
        /// If no data is available from the device, it will attempt to reset the device.
        /// </remarks>
        public void Start(string channelId = "CH1")
        {
            int channelIndex = GetDataAcquisitionChannelIndex(channelId);

            terminateAcquisition = false;
            bool bufferErrorOccurred = false; // Renamed to avoid conflict

            samplesBuffer = new ShiftingBuffer(BufferSize, BlockSize, Samplerate)
            {
                ErrorHandlingMode = _bufferErrorHandling
            };
            samplesBuffer.BlockReceived += SamplesBuffer_BlockReceived;
            samplesBuffer.BufferError += SamplesBuffer_BufferError;

            float totalSamplesProcessed = 0; // Renamed for clarity
            float availableSamples = 0;    // Renamed for clarity
            float corruptedSamples = 0;  // Renamed for clarity
            float lostSamples = 0;       // Renamed for clarity

            Thread acquisitionLoopThread = new(() =>
            {
                int i = 0;
                while (!terminateAcquisition)
                {
                    if (i++ % 100 == 0) // Log summary periodically
                    {
                        if (totalSamplesProcessed > 0 && (corruptedSamples > 0 || lostSamples > 0)) // Log only if there were errors
                        {
                            _logger.LogWarning($"Data acquisition device '{SerialNumber}' error summary: Good: {availableSamples / totalSamplesProcessed:P1} | Corrupted: {corruptedSamples / totalSamplesProcessed:P1} | Lost: {lostSamples / totalSamplesProcessed:P1} (Total samples in period: {totalSamplesProcessed:N0})");
                        }

                        totalSamplesProcessed = 0;
                        availableSamples = 0;
                        corruptedSamples = 0;
                        lostSamples = 0;
                    }

                    byte sts;
                    do // Wait for device to be running
                    {
                        _ = dwf.FDwfAnalogInStatus(dwfHandle, 1, out sts);

                        if (sts is not dwf.DwfStateRunning and not dwf.DwfStateConfig and not dwf.DwfStateArmed)
                        {
                            _logger.LogWarning($"Data acquisition device '{SerialNumber}' entered an unusual state! sts:{sts} - {DwfStateToString(sts)}");
                        }
                        if (sts != dwf.DwfStateRunning) Thread.Sleep(100); // Polling delay
                    } while (sts != dwf.DwfStateRunning && !terminateAcquisition);

                    if (terminateAcquisition) break;

                    _ = dwf.FDwfAnalogInStatusRecord(dwfHandle, out int cAvailable, out int cLost, out int cCorrupted);

                    if (cAvailable == 0 && !terminateAcquisition) // No data available, but not terminating
                    {
                        _logger.LogWarning($"Acquisition error on '{SerialNumber}': No data available (cAvailable: {cAvailable:N0}). Retrying device reset.");
                        Thread.Sleep(500); // Brief pause before reset

                        _logger.LogTrace($"Resetting device '{SerialNumber}'...");
                        if (ResetDevice() == null)
                        {
                            _logger.LogError($"Failed to reset device '{SerialNumber}'. Terminating acquisition loop.");
                            break; // Exit if reset fails
                        }

                        bufferErrorOccurred = false; // Reset error flag
                        samplesBuffer.Clear();
                        continue;
                    }

                    int cTotal = cAvailable + cLost + cCorrupted;
                    if (cLost > 0 || cCorrupted > 0)
                    {
                        bufferErrorOccurred = true;
                    }

                    totalSamplesProcessed += cTotal;
                    availableSamples += cAvailable;
                    lostSamples += cLost;
                    corruptedSamples += cCorrupted;

                    if (cAvailable > voltData.Length)
                    {
                        _logger.LogWarning($"Device '{SerialNumber}' provided more data ({cAvailable}) than local buffer ({voltData.Length}). Increasing buffer size.");
                        voltData = new double[cAvailable]; // Resize to what's available now
                    }

                    _ = dwf.FDwfAnalogInStatusData(dwfHandle, channelIndex, voltData, cAvailable);

                    if (bufferErrorOccurred)
                    {
                        samplesBuffer.Error(cAvailable, cLost, cCorrupted, cTotal);
                        bufferErrorOccurred = false; // Reset after handling
                        continue;
                    }

                    double[] voltDataAvailable = new double[cAvailable];
                    Array.Copy(voltData, voltDataAvailable, Math.Min(cAvailable, voltData.Length));
                    _ = samplesBuffer.Write(Array.ConvertAll(voltDataAvailable, v => (float)v));
                }

                CloseDevice();
                samplesBuffer?.Clear(); // Ensure samplesBuffer is not null
                _logger.LogTrace($"Acquisition loop done on '{SerialNumber}'.");
            })
            {
                Priority = ThreadPriority.Highest,
                Name = $"AcquisitionLoop-{SerialNumber}" // Give the thread a descriptive name
            };

            acquisitionLoopThread.Start();
        }

        private void SamplesBuffer_BufferError(object sender, BufferErrorEventArgs e)
        {
            BufferError?.Invoke(this, e);
        }

        private void SamplesBuffer_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            foreach (KeyValuePair<int, List<Action<object, BlockReceivedEventArgs>>> subscribedActionList in subscribers)
            {
                if (e.DataBlock.EndIndex % subscribedActionList.Key == 0)
                {
                    DataBlock db = e.Buffer.GetBlocks(subscribedActionList.Key, e.DataBlock.EndIndex);

                    foreach (Action<object, BlockReceivedEventArgs> subscribedAction in subscribedActionList.Value)
                    {
                        // Consider using Task.Run for CPU-bound work, or if subscribedAction is async itself.
                        // For simple, fast event handlers, direct invocation might be okay.
                        _ = Task.Run(() => subscribedAction(sender, new BlockReceivedEventArgs(e.Buffer, db, _session, e.Error)));
                    }
                }
            }
        }

        /// <summary>
        /// Subscribes an action to be called when a new data block is received, at a specified interval.
        /// The interval will be adjusted to the closest multiple of the underlying block length if necessary.
        /// </summary>
        /// <param name="interval">The desired interval in seconds for receiving data blocks. This will be rounded to the nearest multiple of the acquisition block length.</param>
        /// <param name="action">The action to execute when a data block is received. The action takes the sender and <see cref="BlockReceivedEventArgs"/> as parameters.</param>
        /// <remarks>
        /// If the provided <paramref name="interval"/> is not a multiple of the underlying acquisition block length,
        /// it will be adjusted to the closest valid multiple. If the interval is less than one block length,
        /// it will be rounded up to one block length.
        /// Subscribed actions are executed asynchronously on a background thread.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if the specified interval is too low for the current block length or results in a zero calling frequency.</exception>
        public void SubscribeToBlockReceived(float interval, Action<object, BlockReceivedEventArgs> action)
        {
            double samplesInInterval = Samplerate * interval;

            if (samplesInInterval % BlockSize != 0)
            {
                float newInterval;
                if (samplesInInterval >= BlockSize)
                {
                    double samplesInIntervalRounded = Math.Round(samplesInInterval / BlockSize) * BlockSize;
                    newInterval = (float)(samplesInIntervalRounded / Samplerate);

                    _logger.LogWarning($"The interval {interval}s would have produced a sample count ({samplesInInterval:F2}) not a multiple of the block size ({BlockSize}), so it was modified to {newInterval:F3}s.");
                }
                else // samplesInInterval < BlockSize
                {
                    // If desired interval is less than one block, round up to one block length.
                    newInterval = BlockLength;
                     _logger.LogWarning($"The interval {interval}s is less than the block length ({BlockLength}s). It has been adjusted to {newInterval:F3}s.");
                }
                interval = newInterval;
            }

            int callingFrequencyFactor = (int)Math.Max(1, Math.Round(Samplerate * interval / BlockSize)); // Ensure at least 1

            if (!subscribers.ContainsKey(callingFrequencyFactor))
            {
                subscribers.Add(callingFrequencyFactor, new List<Action<object, BlockReceivedEventArgs>>());
            }

            subscribers[callingFrequencyFactor].Add(action);
            _logger.LogInformation($"Action subscribed to run every {callingFrequencyFactor} data blocks (approx. every {interval:F3} seconds).");
        }

        /// <summary>
        /// Stops the data acquisition loop.
        /// </summary>
        public void Stop()
        {
            terminateAcquisition = true;
            _logger.LogInformation($"Stop acquisition requested for device '{SerialNumber}'.");
        }

        /// <summary>
        /// Clears all data from the internal samples buffer.
        /// </summary>
        public void ClearBuffer()
        {
            samplesBuffer?.Clear();
            _logger.LogInformation($"Buffer cleared for device '{SerialNumber}'.");
        }

        /// <summary>
        /// Changes the parameters of the signal generator on the specified channel.
        /// </summary>
        /// <param name="channelId">The identifier of the signal generator channel (e.g., "W1", "W2").</param>
        /// <param name="function">The waveform function to generate (e.g., Sine, Square).</param>
        /// <param name="startFrequency">The starting frequency in Hz. For static frequency, set <paramref name="endFrequency"/> to null.</param>
        /// <param name="endFrequency">Optional. The ending frequency in Hz for a frequency sweep. If null, frequency remains static at <paramref name="startFrequency"/>.</param>
        /// <param name="isFrequencyPingPong">If true and sweeping, the frequency will sweep back and forth (triangle modulation). If false, it will ramp (sawtooth modulation).</param>
        /// <param name="startAmplitude">The starting amplitude in Volts. For static amplitude, set <paramref name="endAmplitude"/> to null.</param>
        /// <param name="endAmplitude">Optional. The ending amplitude in Volts for an amplitude sweep. If null, amplitude remains static at <paramref name="startAmplitude"/>.</param>
        /// <param name="isAmplitudePingPong">If true and sweeping, the amplitude will sweep back and forth. If false, it will ramp.</param>
        /// <param name="duration">Optional. The duration of one sweep cycle. If null, the signal generator runs continuously with the initial parameters or until stopped.</param>
        /// <exception cref="ArgumentException">Thrown if an invalid <paramref name="channelId"/> or <paramref name="function"/> is provided.</exception>
        public void ChangeSignalGenerator(string channelId, SignalFunction function, float startFrequency, float? endFrequency, bool isFrequencyPingPong, float startAmplitude, float? endAmplitude, bool isAmplitudePingPong, TimeSpan? duration)
        {
            // get the signal generator channel index
            byte signalGeneratorChannelIndex = GetSignalGeneratorChannelIndex(channelId);

            float middleFrequency = endFrequency.HasValue ? (startFrequency + endFrequency.Value) / 2 : startFrequency;
            float middleAmplitude = endAmplitude.HasValue ? (startAmplitude + endAmplitude.Value) / 2 : startAmplitude;

            // stop the signal generator
            _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 0);

            // change the carrier signal function, frequency and amplitude
            _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 1);
            _ = dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, function == SignalFunction.Sine ? dwf.funcSine : function == SignalFunction.Square ? dwf.funcSquare : throw new ArgumentException($"The signal function '{function}' is not valid.", nameof(function)));
            _ = dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, middleFrequency);
            _ = dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, middleAmplitude);
            _ = dwf.FDwfAnalogOutNodeOffsetSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 0.0f);

            if (endFrequency.HasValue && duration.HasValue && duration.Value.TotalSeconds > 0)
            {
                // enable the frequency modulation
                byte fmFunction = isFrequencyPingPong ? dwf.funcTriangle : startFrequency < endFrequency.Value ? dwf.funcRampUp : dwf.funcRampDown;
                double fmSymmetry = (fmFunction == dwf.funcRampUp) ? 100 : (fmFunction == dwf.funcRampDown) ? 0 : 50;
                double fmPhase = (fmFunction == dwf.funcTriangle) ? 270 : 0;

                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 1);
                _ = dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, fmFunction);
                _ = dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 1.0 / duration.Value.TotalSeconds);
                _ = dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 100.0 * (endFrequency.Value - middleFrequency) / middleFrequency);
                _ = dwf.FDwfAnalogOutNodeSymmetrySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, fmSymmetry);
                _ = dwf.FDwfAnalogOutNodePhaseSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, fmPhase);
            }
            else
            {
                // disable the frequency modulation
                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 0);
            }

            if (endAmplitude.HasValue && duration.HasValue && duration.Value.TotalSeconds > 0)
            {
                // enable the amplitude modulation
                byte amFunction = isAmplitudePingPong ? dwf.funcTriangle : startAmplitude < endAmplitude.Value ? dwf.funcRampUp : dwf.funcRampDown;
                double amSymmetry = (amFunction == dwf.funcRampUp) ? 100 : (amFunction == dwf.funcRampDown) ? 0 : 50;
                double amPhase = (amFunction == dwf.funcTriangle) ? 270 : 0;

                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 1);
                _ = dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, amFunction);
                _ = dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 1.0 / duration.Value.TotalSeconds);
                _ = dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 100.0 * (endAmplitude.Value - middleAmplitude) / middleAmplitude);
                _ = dwf.FDwfAnalogOutNodeSymmetrySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, amSymmetry);
                _ = dwf.FDwfAnalogOutNodePhaseSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, amPhase);
            }
            else
            {
                // disable the amplitude modulation
                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 0);
            }

            if (duration.HasValue && duration.Value.TotalSeconds > 0)
            {
                // set the signal generator repeat duration
                _ = dwf.FDwfAnalogOutRunSet(dwfHandle, signalGeneratorChannelIndex, duration.Value.TotalSeconds);
                _ = dwf.FDwfAnalogOutRepeatSet(dwfHandle, signalGeneratorChannelIndex, 1); // Repeat once for the specified duration
            }
            else // Continuous run
            {
                _ = dwf.FDwfAnalogOutRunSet(dwfHandle, signalGeneratorChannelIndex, 0); // Run indefinitely
                _ = dwf.FDwfAnalogOutRepeatSet(dwfHandle, signalGeneratorChannelIndex, 0); // Repeat indefinitely
            }

            _logger.LogTrace($"Generating {function} wave on channel {channelId} (HW Index: {signalGeneratorChannelIndex}). Freq: {startFrequency:N}Hz{(endFrequency.HasValue ? $" to {endFrequency.Value:N}Hz" : "")}, Amp: {startAmplitude:N}V{(endAmplitude.HasValue ? $" to {endAmplitude.Value:N}V" : "")}, Duration: {(duration.HasValue ? $"{duration.Value.TotalSeconds:N2}s" : "Continuous")}");

            _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
            // Apply changes (3) if running, otherwise start (1)
            _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, (psts == dwf.DwfStateRunning) ? 3 : 1);

            ApplySignalGeneratorChanges(signalGeneratorChannelIndex);
        }

        /// <summary>
        /// Stops the signal generator on the specified channel.
        /// </summary>
        /// <param name="channelId">The identifier of the signal generator channel to stop (e.g., "W1", "W2").</param>
        public void StopSignalGenerator(string channelId)
        {
            // get the signal generator channel index
            byte signalGeneratorChannelIndex = GetSignalGeneratorChannelIndex(channelId);

            _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
            if (psts != dwf.DwfStateRunning)
            {
                _logger.LogWarning($"The signal generator on channel {channelId} is not currently running.");
            }

            // Stop the signal generator
            _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 0);

            ApplySignalGeneratorChanges(signalGeneratorChannelIndex);
            _logger.LogInformation($"Signal generator stopped on channel {channelId}.");
        }

        /// <summary>
        /// Gets the current status of the signal generator on the specified channel.
        /// </summary>
        /// <param name="channelId">The identifier of the signal generator channel (e.g., "W1", "W2").</param>
        /// <returns>A <see cref="SignalGeneratorStatus"/> object containing the current state, frequency, and amplitude of the signal generator.</returns>
        public SignalGeneratorStatus GetSignalGeneratorStatus(string channelId)
        {
            SignalGeneratorStatus signalGeneratorStatus = new()
            {
                ChannelId = channelId
            };

            // get the signal generator channel index
            byte signalGeneratorChannelIndex = GetSignalGeneratorChannelIndex(channelId);
            signalGeneratorStatus.ChannelIndex = signalGeneratorChannelIndex;

            _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
            signalGeneratorStatus.State = psts;
            signalGeneratorStatus.IsRunning = psts == dwf.DwfStateRunning;
            if (!signalGeneratorStatus.IsRunning)
            {
                _logger.LogWarning($"The signal generator on channel {channelId} is not running (State: {DwfStateToString(psts)}). Reported frequency and amplitude may not be active.");
            }

            // get the current frequency of the signal generator
            _ = dwf.FDwfAnalogOutNodeFrequencyGet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, out double frequency);
            signalGeneratorStatus.Frequency = frequency;

            // get the current amplitude of the signal generator
            _ = dwf.FDwfAnalogOutNodeAmplitudeGet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, out double amplitude);
            signalGeneratorStatus.Amplitude = amplitude;

            return signalGeneratorStatus;
        }

        private static byte GetSignalGeneratorChannelIndex(string channelId)
        {
            return channelId switch
            {
                "W1" => 0,
                "W2" => 1,
                _ => throw new ArgumentException($"The signal generator channel id '{channelId}' is not valid. Valid options are 'W1' or 'W2'.", nameof(channelId)),
            };
        }

        private static byte GetDataAcquisitionChannelIndex(string channelId)
        {
            return channelId switch
            {
                "CH1" => 0,
                "CH2" => 1,
                _ => throw new ArgumentException($"The data acquisition channel id '{channelId}' is not valid. Valid options are 'CH1' or 'CH2'.", nameof(channelId)),
            };
        }

        private void ApplySignalGeneratorChanges(byte signalGeneratorChannelIndex)
        {
            // It can take a moment for the device to report the new state after configuration.
            // This loop provides a brief period to observe the state transition.
            for (int attempt = 0; attempt < 5; attempt++) // Try up to 5 times with short delays
            {
                _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
                _logger.LogTrace($"Signal generator on channel index {signalGeneratorChannelIndex} status after configure attempt {attempt + 1}: {DwfStateToString(psts)}");

                // If the desired state (or a stable intermediate state) is reached, we can break.
                // For now, just logging and a small delay is sufficient.
                if (psts == dwf.DwfStateRunning || psts == dwf.DwfStateReady || psts == dwf.DwfStateDone) // Added Ready/Done as stable states
                {
                    break;
                }
                Thread.Sleep(10); // Shorter delay
            }
            // Final short delay to ensure settings are applied.
            Thread.Sleep(40); // Reduced from 50 to make total 5*10+40 = 90ms max for this part.
        }

        private string DwfStateToString(byte psts) // Made private as it's a helper for this class
        {
            return psts switch
            {
                dwf.DwfStateReady => "Ready",
                dwf.DwfStateConfig => "Config", // Note: DwfStateConfig is 4
                dwf.DwfStatePrefill => "Prefill",
                dwf.DwfStateArmed => "Armed",
                dwf.DwfStateWait => "Wait", // Note: DwfStateWait is 7
                dwf.DwfStateTriggered => "Triggered/Running", // DwfStateTriggered and DwfStateRunning are both 3
                dwf.DwfStateDone => "Done",
                _ => $"Unknown ({psts})",
            };
        }
    }
}