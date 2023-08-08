using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Sessions;
using static BBD.BodyMonitor.Buffering.ShiftingBuffer;

namespace BBD.BodyMonitor.Models
{
    internal class DataAcquisition
    {
        public int DeviceIndex { get; internal set; }
        public string SerialNumber { get; private set; }
        public float Samplerate { get; private set; }
        /// <summary>
        /// Number of samples to fill the buffer
        /// </summary>
        public int BufferSize { get; internal set; }
        public int BlockSize { get; internal set; }
        public float BlockLength { get; private set; }
        public float BufferLength { get; private set; }
        public string[] AcquisitionChannels { get; private set; }
        private BufferErrorHandlingMode _bufferErrorHandling;
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
        /// Number of samples per buffer
        /// </summary>
        private int bufferSize;

        private ShiftingBuffer samplesBuffer;
        public event BufferErrorEventHandler BufferError;
        private double[] voltData;

        private bool terminateAcquisition = false;

        private readonly Dictionary<int, List<Action<object, BlockReceivedEventArgs>>> subscribers = new();

        public DataAcquisition(ILogger logger, Session session)
        {
            _logger = logger;
            _session = session;

            logger.LogInformation($"DWF Version: {GetDwfVersion()}");
        }

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
        /// Open the device and set up the acquisition.
        /// </summary>
        /// <param name="deviceIndex">Index of the device, or -1 for the default device</param>
        /// <param name="acquisitionChannels"></param>
        /// <param name="acquisitionSamplerate"></param>
        /// <param name="signalGeneratorEnabled"></param>
        /// <param name="signalGeneratorChannel"></param>
        /// <param name="signalGeneratorFrequency"></param>
        /// <param name="signalGeneratorVoltage"></param>
        /// <param name="blockLength"></param>
        /// <param name="bufferLength"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
            bufferSize = Math.Min(bufferSizeMaximum, (int)acquisitionSamplerate);
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
                throw new Exception($"Buffer size {bufferLength} and {blockLength} must be multiples. Adjust your settings!");
            }
            int blockCountInBuffer = (int)(bufferLength / blockLength);

            BlockSize = (int)Math.Floor(Samplerate * blockLength);
            BufferSize = blockCountInBuffer * BlockSize;
            subscribers.Clear();

            _ = dwf.FDwfAnalogInBufferSizeSet(dwfHandle, bufferSize);
            _ = dwf.FDwfAnalogInAcquisitionModeSet(dwfHandle, dwf.acqmodeRecord);
            _ = dwf.FDwfAnalogInRecordLengthSet(dwfHandle, -1);

            foreach (string channelId in acquisitionChannels)
            {
                byte acquisitionChannelIndex = GetDataAcquisitionChannelIndex(channelId);

                _ = dwf.FDwfAnalogInChannelEnableSet(dwfHandle, acquisitionChannelIndex, 1);
                _ = dwf.FDwfAnalogInChannelRangeSet(dwfHandle, acquisitionChannelIndex, 5.0);
            }

            // wait at least 2 seconds for the offset to stabilize
            Thread.Sleep(2000);

            // start data acquisition on all channels
            _ = dwf.FDwfAnalogInConfigure(dwfHandle, 0, 1);

            return serialNumber;
        }

        public void CloseDevice()
        {
            _ = dwf.FDwfDeviceClose(dwfHandle);
        }

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
        /// Start the data acquisition.
        /// </summary>
        public void Start(string channelId = "CH1")
        {
            int channelIndex = GetDataAcquisitionChannelIndex(channelId);

            terminateAcquisition = false;
            bool bufferError = false;

            samplesBuffer = new ShiftingBuffer(BufferSize, BlockSize, Samplerate)
            {
                ErrorHandlingMode = _bufferErrorHandling
            };
            samplesBuffer.BlockReceived += SamplesBuffer_BlockReceived;
            samplesBuffer.BufferError += SamplesBuffer_BufferError;

            float totalBytes = 0;
            float availableBytes = 0;
            float corruptedBytes = 0;
            float lostBytes = 0;

            Thread acquisitionLoopThread = new(() =>
            {
                int i = 0;
                while (!terminateAcquisition)
                {
                    if (i++ % 100 == 0)
                    {
                        if (availableBytes != totalBytes)
                        {
                            _logger.LogWarning($"Data acquisition device '{SerialNumber}' reported some errors! Good: {availableBytes / totalBytes,6:0.0%} | Corrupted: {corruptedBytes / totalBytes,6:0.0%} | Lost: {lostBytes / totalBytes,6:0.0%}");
                        }

                        totalBytes = 0;
                        availableBytes = 0;
                        corruptedBytes = 0;
                        lostBytes = 0;
                    }

                    while (true)
                    {
                        _ = dwf.FDwfAnalogInStatus(dwfHandle, 1, out byte sts);

                        if (sts == dwf.DwfStateRunning)
                        {
                            break;
                        }

                        if (sts is not dwf.DwfStateConfig and not dwf.DwfStateArmed)
                        {
                            _logger.LogWarning($"Data acquisition device '{SerialNumber}' got into an unusual state! sts:{sts} - {DwfStateToString(sts)}");
                        }
                        Thread.Sleep(100);
                    }

                    _ = dwf.FDwfAnalogInStatusRecord(dwfHandle, out int cAvailable, out int cLost, out int cCorrupted);

                    if (cAvailable == 0)
                    {
                        _logger.LogWarning($"Aqusition error! cAvailable: {cAvailable:N0}");
                        Thread.Sleep(500);

                        _logger.LogTrace($"Reseting device...");
                        _ = ResetDevice();

                        bufferError = false;
                        samplesBuffer.Clear();
                        continue;
                    }

                    int cTotal = cAvailable + cLost + cCorrupted;
                    if (cLost > 0 || cCorrupted > 0)
                    {
                        bufferError = true;
                    }

                    totalBytes += cTotal;
                    availableBytes += cAvailable;
                    lostBytes += cLost;
                    corruptedBytes += cCorrupted;

                    if (cAvailable > voltData.Length)
                    {
                        _logger.LogWarning($"The data available on the device is more than our local buffer size ({cAvailable} > {voltData.Length}), so we double the size of the buffer.");
                        voltData = new double[voltData.Length * 2];
                    }

                    _ = dwf.FDwfAnalogInStatusData(dwfHandle, channelIndex, voltData, cAvailable);     // get CH1/CH2 data chunk

                    if (bufferError)
                    {
                        samplesBuffer.Error(cAvailable, cLost, cCorrupted, cTotal);
                        bufferError = false;
                        continue;
                    }

                    double[] voltDataAvailable = new double[cAvailable];
                    Array.Copy(voltData, voltDataAvailable, Math.Min(cAvailable, voltData.Length));
                    _ = samplesBuffer.Write(Array.ConvertAll(voltDataAvailable, v => (float)v));
                }

                CloseDevice();
                samplesBuffer.Clear();
                _logger.LogTrace($"Acquisition done on '{SerialNumber}'");
            })
            {
                Priority = ThreadPriority.Highest
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
                        _ = Task.Run(() => subscribedAction(sender, new BlockReceivedEventArgs(e.Buffer, db, _session, e.Error)));
                    }
                }
            }
        }

        public void SubscribeToBlockReceived(float interval, Action<object, BlockReceivedEventArgs> action)
        {
            double samplesInInterval = Samplerate * interval;

            if (samplesInInterval % BlockSize != 0)
            {
                if (samplesInInterval >= BlockSize)
                {
                    double samplesInIntervalRounded = Math.Round(samplesInInterval / BlockSize) * BlockSize;
                    float newInterval = (float)(samplesInIntervalRounded / Samplerate);

                    _logger.LogWarning($"The interval {interval} would have produced a sample count that is not a multiple of the block size {BlockSize}, so it was modified to {newInterval}.");

                    interval = newInterval;
                }
                else
                {
                    throw new ArgumentException("The interval must be a multiple of the block length.", "interval");
                }
            }

            int callingFrequency = (int)(Samplerate * interval / BlockSize);

            if (callingFrequency == 0)
            {
                throw new ArgumentException($"The interval ({interval:N0} seconds) is too low for this block length ({BlockSize} samples).", "interval");
            }

            if (!subscribers.ContainsKey(callingFrequency))
            {
                subscribers.Add(callingFrequency, new List<Action<object, BlockReceivedEventArgs>>());
            }

            subscribers[callingFrequency].Add(action);
        }

        /// <summary>
        /// Stop the data acquisition.
        /// </summary>
        public void Stop()
        {
            terminateAcquisition = true;
        }

        public void ClearBuffer()
        {
            samplesBuffer.Clear();
        }

        public void ChangeSingalGenerator(string channelId, SignalFunction function, float startFrequency, float? endFrequency, bool isFrequencyPingPong, float startAmplitude, float? endAmplitude, bool isAmplitudePingPong, TimeSpan? duration)
        {
            // get the signal generator channel index
            byte signalGeneratorChannelIndex = GetSignalGeneratorChannelIndex(channelId);

            float middleFrequency = endFrequency.HasValue ? (startFrequency + endFrequency.Value) / 2 : startFrequency;
            float middleAmplitude = endAmplitude.HasValue ? (startAmplitude + endAmplitude.Value) / 2 : startAmplitude;

            // stop the signal generator
            _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 0);

            // change the carrier signal function, frequency and amplitude
            _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 1);
            _ = dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, function == SignalFunction.Sine ? dwf.funcSine : function == SignalFunction.Square ? dwf.funcSquare : throw new ArgumentException($"The signal function '{function}' is not valid.", "function"));
            _ = dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, middleFrequency);
            _ = dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, middleAmplitude);
            _ = dwf.FDwfAnalogOutNodeOffsetSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 0.0f);

            if (endFrequency.HasValue && duration.HasValue)
            {
                // enable the frequency modulation
                byte fmFunction = isFrequencyPingPong ? dwf.funcTriangle : startFrequency < endFrequency.Value ? dwf.funcRampUp : dwf.funcRampDown;
                double fmSimmetry = 100;
                double fmPhase = 0;
                if (fmFunction == dwf.funcRampUp)
                {
                    fmSimmetry = 100;
                }
                else if (fmFunction == dwf.funcRampDown)
                {
                    fmSimmetry = 0;
                }
                else if (fmFunction == dwf.funcTriangle)
                {
                    fmSimmetry = 50;
                    fmPhase = 270;
                }

                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 1);
                _ = dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, fmFunction);
                _ = dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 1.0 / duration.Value.TotalSeconds);
                _ = dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 100.0 * (endFrequency.Value - middleFrequency) / middleFrequency);
                _ = dwf.FDwfAnalogOutNodeSymmetrySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, fmSimmetry);
                _ = dwf.FDwfAnalogOutNodePhaseSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, fmPhase);
            }
            else
            {
                // disable the frequency modulation
                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 0);
            }

            if (endAmplitude.HasValue && duration.HasValue)
            {
                // enable the amplitude modulation
                byte amFunction = isAmplitudePingPong ? dwf.funcTriangle : dwf.funcRampUp;
                double amSimmetry = 100;
                double amPhase = 0;
                if (amFunction == dwf.funcRampUp)
                {
                    amSimmetry = 100;
                }
                else if (amFunction == dwf.funcRampDown)
                {
                    amSimmetry = 0;
                }
                else if (amFunction == dwf.funcTriangle)
                {
                    amSimmetry = 50;
                    amPhase = 270;
                }

                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 1);
                _ = dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, amFunction);
                _ = dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 1.0 / duration.Value.TotalSeconds);
                _ = dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 100.0 * (endAmplitude.Value - middleAmplitude) / middleAmplitude);
                _ = dwf.FDwfAnalogOutNodeSymmetrySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, amSimmetry);
                _ = dwf.FDwfAnalogOutNodePhaseSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, amPhase);
            }
            else
            {
                // disable the amplitude modulation
                _ = dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeAM, 0);
            }

            if (duration.HasValue)
            {
                // set the signal generator repeat duration
                _ = dwf.FDwfAnalogOutRunSet(dwfHandle, signalGeneratorChannelIndex, duration.Value.TotalSeconds);
                _ = dwf.FDwfAnalogOutRepeatSet(dwfHandle, signalGeneratorChannelIndex, 1);
            }

            _logger.LogTrace($"Generating {function} wave at {startFrequency:N} Hz with {startAmplitude:N} V of amplitude at channel {signalGeneratorChannelIndex}...");

            _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
            if (psts != dwf.DwfStateRunning)
            {
                // start the signal generator
                _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 1);
            }
            else
            {
                // apply changes to the signal generator
                _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 3);
            }

            ApplySignalGeneratorChanges(signalGeneratorChannelIndex);
        }

        public void StopSingalGenerator(string channelId)
        {
            // get the signal generator channel index
            byte signalGeneratorChannelIndex = GetSignalGeneratorChannelIndex(channelId);

            _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
            if (psts != dwf.DwfStateRunning)
            {
                _logger.LogWarning($"The signal generator on channel {channelId} is not running.");
            }

            // apply changes to the signal generator
            _ = dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 0);

            ApplySignalGeneratorChanges(signalGeneratorChannelIndex);
        }

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
            if (psts != dwf.DwfStateRunning)
            {
                _logger.LogWarning($"The signal generator on channel {channelId} is not running.");
            }
            signalGeneratorStatus.State = psts;
            signalGeneratorStatus.IsRunning = psts == dwf.DwfStateRunning;

            // get the current frequemcy of the signal generator 
            _ = dwf.FDwfAnalogOutNodeFrequencyGet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, out double frequency);
            signalGeneratorStatus.Frequency = frequency;

            // get the current amplitude of the signal generator
            _ = dwf.FDwfAnalogOutNodeAmplitudeGet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, out double amplitude);
            signalGeneratorStatus.Amplitude = amplitude;

            return signalGeneratorStatus;
        }

        private static byte GetSignalGeneratorChannelIndex(string channelId)
        {
            byte signalGeneratorChannelIndex = channelId switch
            {
                "W1" => 0,
                "W2" => 1,
                _ => throw new ArgumentException($"The signal generator channel id '{channelId}' is not valid.", "channelId"),
            };
            return signalGeneratorChannelIndex;
        }

        private static byte GetDataAcquisitionChannelIndex(string channelId)
        {
            byte dataAcquisitionChannelIndex = channelId switch
            {
                "CH1" => 0,
                "CH2" => 1,
                _ => throw new ArgumentException($"The data acquisition channel id '{channelId}' is not valid.", "channelId"),
            };
            return dataAcquisitionChannelIndex;
        }

        private void ApplySignalGeneratorChanges(byte signalGeneratorChannelIndex)
        {
            while (true)
            {
                _logger.LogTrace($"FDwfAnalogOutStatus begin | dwfHandle:{dwfHandle}");
                _ = dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
                _logger.LogTrace($"FDwfAnalogOutStatus end   | sts:{psts} - {DwfStateToString(psts)}");

                if (psts == dwf.DwfStateRunning)
                {
                    break;
                }

                _logger.LogWarning($"We got into an unusual signal generator state! sts:{psts} - {DwfStateToString(psts)}");
                Thread.Sleep(500);
                break;
            }

            // signal generator needs a little time to start
            Thread.Sleep(50);
        }

        private object DwfStateToString(byte psts)
        {
            return psts switch
            {
                0 => "Ready",
                1 => "Config",
                2 => "Prefill",
                3 => "Armed",
                4 => "Wait",
                5 => "Triggered",
                6 => "Running",
                _ => "Unknown",
            };
        }
    }
}