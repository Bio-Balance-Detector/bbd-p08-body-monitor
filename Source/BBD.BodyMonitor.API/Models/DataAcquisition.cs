using BBD.BodyMonitor.Buffering;
using BBD.BodyMonitor.Environment;
using Fitbit.Models;
using static BBD.BodyMonitor.Buffering.ShiftingBuffer;

namespace BBD.BodyMonitor
{
    internal class DataAcquisition
    {
        public float Samplerate { get; private set; }
        /// <summary>
        /// Number of samples to fill the buffer
        /// </summary>
        public int BufferSize { get; internal set; }
        public int BlockSize { get; internal set; }
        public bool SignalGeneratorEnabled { get; private set; }
        public byte SignalGeneratorChannel { get; private set; }
        public float SignalGeneratorFrequency { get; private set; }
        public float SignalGeneratorVoltage { get; private set; }
        public float BlockLength { get; private set; }
        public float BufferLength { get; private set; }
        public int[] AcquisitionChannels { get; private set; }

        private ILogger logger;

        private int dwfHandle = -1;

        /// <summary>
        /// Number of samples per buffer
        /// </summary>
        private int bufferSize;

        private ShiftingBuffer samplesBuffer;
        public event BufferErrorEventHandler BufferError;
        private double[] voltData;

        private bool terminateAcquisition = false;

        private Dictionary<int, List<Action<object, BlockReceivedEventArgs>>> subscribers = new Dictionary<int, List<Action<object, BlockReceivedEventArgs>>>();

        public DataAcquisition(ILogger logger)
        {
            this.logger = logger;

            dwf.FDwfGetVersion(out string dwfVersion);
            logger.LogInformation($"DWF Version: {dwfVersion}");
        }

        public ConnectedDevice[] ListDevices()
        {
            List<ConnectedDevice> result = new List<ConnectedDevice>();

            dwf.FDwfGetVersion(out string dwfVersion);

            dwf.FDwfEnum(dwf.enumfilterAll, out int deviceCount);
            for (int idxDevice = 0; idxDevice < deviceCount; idxDevice++)
            {
                ConnectedDevice device = new ConnectedDevice();

                device.Brand = "Digilent";
                device.Library = "DWF v" + dwfVersion;

                dwf.FDwfEnumDeviceType(idxDevice, out int deviceId, out int deviceRevision);
                device.Id = deviceId;
                device.Revision = deviceRevision;

                dwf.FDwfEnumDeviceIsOpened(idxDevice, out int isOpened);
                device.IsOpened = isOpened == 1;

                dwf.FDwfEnumUserName(idxDevice, out string userName);
                device.UserName = userName;

                dwf.FDwfEnumDeviceName(idxDevice, out string deviceName);
                device.Name = deviceName;

                dwf.FDwfEnumSN(idxDevice, out string serialNumber);
                device.SerialNumber = serialNumber;

                result.Add(device);
            }

            return result.ToArray();
        }

        public int OpenDevice(int[] acquisitionChannels, float acquisitionSamplerate, bool signalGeneratorEnabled, byte signalGeneratorChannel, float signalGeneratorFrequency, float signalGeneratorVoltage, float blockLength, float bufferLength)
        {
            this.AcquisitionChannels = acquisitionChannels;
            this.Samplerate = acquisitionSamplerate;
            this.SignalGeneratorEnabled = signalGeneratorEnabled;
            this.SignalGeneratorChannel = signalGeneratorChannel;
            this.SignalGeneratorFrequency = signalGeneratorFrequency;
            this.SignalGeneratorVoltage = signalGeneratorVoltage;
            this.BlockLength = blockLength;
            this.BufferLength = bufferLength;

            // Open the (first) AD2 with the 2nd configuration with 16k analog-in buffer
            dwf.FDwfDeviceConfigOpen(-1, 1, out dwfHandle);

            while ((dwfHandle == dwf.hdwfNone) && (!terminateAcquisition))
            {
                dwf.FDwfGetLastErrorMsg(out string lastError);
                logger.LogWarning($"Failed to open device: {lastError.TrimEnd()}. Retrying in 10 seconds.");

                Thread.Sleep(10000);

                dwf.FDwfDeviceConfigOpen(-1, 1, out dwfHandle);
            }

            dwf.FDwfAnalogInBufferSizeInfo(dwfHandle, out int bufferSizeMinimum, out int bufferSizeMaximum);
            bufferSize = Math.Min(bufferSizeMaximum, (int)acquisitionSamplerate);
            logger.LogTrace($"Device buffer size range: {bufferSizeMinimum:N0} - {bufferSizeMaximum:N0} samples, set to {bufferSize:N0}.");
            voltData = new double[bufferSize];

            //set up acquisition
            dwf.FDwfAnalogInFrequencySet(dwfHandle, acquisitionSamplerate);
            dwf.FDwfAnalogInFrequencyGet(dwfHandle, out double realSamplerate);

            this.Samplerate = (float)realSamplerate;
            if (acquisitionSamplerate != (int)realSamplerate)
            {
                logger.LogWarning($"The sampling rate of {acquisitionSamplerate:N} Hz is not supported, so the effective sampling rate was set to {realSamplerate:N} Hz. Native sampling rates for AD2 are the following: 1, 2, 4, 5, 8, 10, 16, 20, 25, 32, 40, 50, 64, 80, 100, 125, 128, 160, 200, 250, 256, 320, 400, 500, 625, 640, 800 Hz, 1, 1.25, 1.28, 1.6, 2, 2.5, 3.125, 3.2, 4, 5, 6.25, 6.4, 8, 10, 12.5, 15.625, 16, 20, 25, 31.250, 32, 40, 50, 62.5, 78.125, 80, 100, 125, 156.25, 160, 200, 250, 312.5, 390.625, 400, 500, 625, 781.25, 800 kHz, 1, 1.25, 1.5625, 2, 2.5, 3.125, 4, 5, 6.25, 10, 12.5, 20, 25, 50 and 100 MHz.");
            }

            if ((bufferLength / blockLength) - Math.Truncate(bufferLength / blockLength) != 0)
            {
                throw new Exception($"Buffer size {bufferLength} and {blockLength} must be multiples. Adjust your settings!");
            }
            int blockCountInBuffer = (int)(bufferLength / blockLength);

            this.BlockSize = (int)Math.Floor(this.Samplerate * blockLength);
            this.BufferSize = blockCountInBuffer * this.BlockSize;
            this.subscribers.Clear();

            dwf.FDwfAnalogInBufferSizeSet(dwfHandle, bufferSize);
            dwf.FDwfAnalogInAcquisitionModeSet(dwfHandle, dwf.acqmodeRecord);
            dwf.FDwfAnalogInRecordLengthSet(dwfHandle, -1);

            this.AcquisitionChannels = acquisitionChannels;
            foreach (byte acquisitionChannel in acquisitionChannels)
            {
                byte acquisitionChannelIndex = 255;
                if (acquisitionChannel == 1)
                {
                    acquisitionChannelIndex = 0;
                }
                else if (acquisitionChannel == 2)
                {
                    acquisitionChannelIndex = 1;
                }

                dwf.FDwfAnalogInChannelEnableSet(dwfHandle, acquisitionChannelIndex, 1);
                dwf.FDwfAnalogInChannelRangeSet(dwfHandle, acquisitionChannelIndex, 5.0);
            }

            if ((signalGeneratorEnabled) && (signalGeneratorChannel > 0))
            {
                // calculate the signal generator channel id
                byte signalGeneratorChannelIndex = 255;
                if (signalGeneratorChannel == 1)
                {
                    signalGeneratorChannelIndex = 0;
                }
                else if (signalGeneratorChannel == 2)
                {
                    signalGeneratorChannelIndex = 1;
                }

                // set up signal generation
                dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 1);
                dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, dwf.funcSine);
                dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, signalGeneratorFrequency);
                dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, signalGeneratorVoltage);

                logger.LogTrace($"Generating sine wave at {signalGeneratorFrequency:N} Hz with {signalGeneratorVoltage} V of amplitude at channel {signalGeneratorChannelIndex}...");
                dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 1);

                this.SignalGeneratorEnabled = signalGeneratorEnabled;
                this.SignalGeneratorChannel = signalGeneratorChannel;
                this.SignalGeneratorFrequency = signalGeneratorFrequency;
                this.SignalGeneratorVoltage = signalGeneratorVoltage;
            }

            //wait at least 2 seconds for the offset to stabilize
            Thread.Sleep(2000);

            //start data acquisition on CH0
            dwf.FDwfAnalogInConfigure(dwfHandle, 0, 1);

            return dwfHandle;
        }

        public void CloseDevice()
        {
            dwf.FDwfDeviceCloseAll();
        }

        public int ResetDevice()
        {
            try
            {
                CloseDevice();
            }
            catch { }

            return OpenDevice(this.AcquisitionChannels, this.Samplerate, this.SignalGeneratorEnabled, this.SignalGeneratorChannel, this.SignalGeneratorFrequency, this.SignalGeneratorVoltage, this.BlockLength, this.BufferLength);
        }

        /// <summary>
        /// Start the data acquisition.
        /// </summary>
        public void Start()
        {
            terminateAcquisition = false;

            int bufferIndex = 0;
            bool bufferError = false;

            samplesBuffer = new ShiftingBuffer(this.BufferSize, this.BlockSize, this.Samplerate);
            samplesBuffer.BlockReceived += SamplesBuffer_BlockReceived;
            samplesBuffer.BufferError += SamplesBuffer_BufferError;

            float totalBytes = 0;
            float availableBytes = 0;
            float corruptedBytes = 0;
            float lostBytes = 0;

            Thread acquisitionLoopThread = new Thread(() =>
            {
                int i = 0;
                while (!terminateAcquisition)
                {
                    if (i++ % 100 == 0)
                    {
                        if (availableBytes != totalBytes)
                        {
                            logger.LogWarning($"Data acquisition device reported some errors! Good: {(availableBytes / totalBytes).ToString("0.0%").PadLeft(6)} | Corrupted: {(corruptedBytes / totalBytes).ToString("0.0%").PadLeft(6)} | Lost: {(lostBytes / totalBytes).ToString("0.0%").PadLeft(6)}");
                        }

                        totalBytes = 0;
                        availableBytes = 0;
                        corruptedBytes = 0;
                        lostBytes = 0;
                    }

                    while (true)
                    {
                        dwf.FDwfAnalogInStatus(dwfHandle, 1, out byte sts);

                        if (sts == dwf.DwfStateRunning)
                        {
                            break;
                        }

                        if (sts != dwf.DwfStateConfig)
                        {
                            logger.LogWarning($"Data acquisition device got into an unusual state! sts:{sts}");
                        }
                        Thread.Sleep(100);
                    }

                    dwf.FDwfAnalogInStatusRecord(dwfHandle, out int cAvailable, out int cLost, out int cCorrupted);

                    if (cAvailable == 0)
                    {
                        logger.LogWarning($"Aqusition error! cAvailable: {cAvailable:N0}");
                        Thread.Sleep(500);

                        logger.LogTrace($"Reseting device...");
                        dwfHandle = ResetDevice();

                        bufferError = false;
                        samplesBuffer.Clear();
                        continue;
                    }

                    int cTotal = cAvailable + cLost + cCorrupted;
                    if ((cLost > 0) || (cCorrupted > 0))
                    {
                        bufferError = true;
                    }

                    totalBytes += cTotal;
                    availableBytes += cAvailable;
                    lostBytes += cLost;
                    corruptedBytes += cCorrupted;

                    dwf.FDwfAnalogInStatusData(dwfHandle, 0, voltData, cAvailable);     // get CH1 data chunk

                    if (bufferError)
                    {
                        samplesBuffer.Error(cAvailable, cLost, cCorrupted, (int)cTotal);
                        bufferError = false;
                        continue;
                    }

                    double[] voltDataAvailable = new double[cAvailable];
                    Array.Copy(voltData, voltDataAvailable, Math.Min(cAvailable, voltData.Length));
                    samplesBuffer.Write(Array.ConvertAll<double, float>(voltDataAvailable, v => (float)v));
                }

                samplesBuffer.Clear();
                logger.LogTrace("Acquisition done");
            });

            acquisitionLoopThread.Priority = ThreadPriority.Highest;
            acquisitionLoopThread.Start();
        }

        private void SamplesBuffer_BufferError(object sender, BufferErrorEventArgs e)
        {
            this.BufferError?.Invoke(this, e);
        }

        private void SamplesBuffer_BlockReceived(object sender, BlockReceivedEventArgs e)
        {
            foreach (var subscribedActionList in subscribers)
            {
                if (e.DataBlock.EndIndex % subscribedActionList.Key == 0)
                {
                    DataBlock db = e.Buffer.GetBlocks(subscribedActionList.Key, e.DataBlock.EndIndex);

                    foreach (var subscribedAction in subscribedActionList.Value)
                    {
                        Task.Run(() => subscribedAction(sender, new BlockReceivedEventArgs(e.Buffer, db)));
                    }
                }
            }
        }

        /// <summary>
        /// Stop the data acquisition.
        /// </summary>
        public void Stop()
        {
            terminateAcquisition = true;
        }

        public void SubscribeToBlockReceived(float interval, Action<object, BlockReceivedEventArgs> action)
        {
            double samplesInInterval = (this.Samplerate * interval);

            if (samplesInInterval % this.BlockSize != 0)
            {
                if (samplesInInterval >= this.BlockSize)
                {
                    double samplesInIntervalRounded = Math.Round(samplesInInterval / this.BlockSize) * this.BlockSize;
                    float newInterval = (float)(samplesInIntervalRounded / this.Samplerate);

                    logger.LogWarning($"The interval {interval} would have produced a sample count that is not a multiple of the block size {this.BlockSize}, so it was modified to {newInterval}.");

                    interval = newInterval;
                }
                else
                {
                    throw new ArgumentException("The interval must be a multiple of the block length.", "interval");
                }
            }

            int callingFrequency = (int)(this.Samplerate * interval / this.BlockSize);

            if (callingFrequency == 0)
            {
                throw new ArgumentException($"The interval ({interval:N0} seconds) is too low for this block length ({this.BlockSize} samples).", "interval");
            }

            if (!subscribers.ContainsKey(callingFrequency))
            {
                subscribers.Add(callingFrequency, new List<Action<object, BlockReceivedEventArgs>>());
            }

            subscribers[callingFrequency].Add(action);
        }

        public void ChangeSingalGeneratorFrequency(float frequency)
        {
            // calculate the signal generator channel id
            byte signalGeneratorChannelIndex = 255;
            if (this.SignalGeneratorChannel == 1)
            {
                signalGeneratorChannelIndex = 0;
            }
            else if (this.SignalGeneratorChannel == 2)
            {
                signalGeneratorChannelIndex = 1;
            }

            // stop the signal generator
            //dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 0);

            // change the frequency
            dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, frequency);
            //dwf.FDwfAnalogOutNodeFrequencyGet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, out double phzFrequency);
            //dwf.FDwfAnalogOutNodeAmplitudeGet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, out double pvAmplitude);
            //logger.LogInformation($"Signal generator output: {pvAmplitude:0.000} V @ {phzFrequency:0.00} Hz");

            // start the signal generator
            dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 1);

            while (true)
            {
                logger.LogTrace($"FDwfAnalogOutStatus begin | dwfHandle:{dwfHandle}");
                dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
                logger.LogTrace($"FDwfAnalogOutStatus end   | psts:{psts}");

                if (psts == dwf.DwfStateRunning)
                    break;

                logger.LogWarning($"We got into an unusual signal generator state! psts:{psts}");
                Thread.Sleep(500);
                break;
            }

            // signal generator needs a little time to start
            Thread.Sleep(50);
        }

        public void SweepSingalGeneratorFrequency(float startFrequency, float endFrequency, float duration)
        {
            float middleFrequency = (startFrequency + endFrequency) / 2;

            // calculate the signal generator channel id
            byte signalGeneratorChannelIndex = 255;
            if (this.SignalGeneratorChannel == 1)
            {
                signalGeneratorChannelIndex = 0;
            }
            else if (this.SignalGeneratorChannel == 2)
            {
                signalGeneratorChannelIndex = 1;
            }

            // stop the signal generator
            dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 0);

            // change the frequency
            dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 1);
            dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, dwf.funcSine);
            dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, middleFrequency);
            dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, this.SignalGeneratorVoltage);
            dwf.FDwfAnalogOutNodeOffsetSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeCarrier, 1.0f);

            dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 1);
            dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, dwf.funcRampUp);
            dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 1.0 / duration);
            dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 100.0 * (endFrequency - middleFrequency) / middleFrequency);
            dwf.FDwfAnalogOutNodeSymmetrySet(dwfHandle, signalGeneratorChannelIndex, dwf.AnalogOutNodeFM, 100);

            dwf.FDwfAnalogOutRunSet(dwfHandle, signalGeneratorChannelIndex, duration);
            dwf.FDwfAnalogOutRepeatSet(dwfHandle, signalGeneratorChannelIndex, 1);

            // start the signal generator
            dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannelIndex, 1);

            while (true)
            {
                logger.LogTrace($"FDwfAnalogOutStatus begin | dwfHandle:{dwfHandle}");
                dwf.FDwfAnalogOutStatus(dwfHandle, signalGeneratorChannelIndex, out byte psts);
                logger.LogTrace($"FDwfAnalogOutStatus end   | psts:{psts}");

                if (psts == dwf.DwfStateRunning)
                    break;

                logger.LogWarning($"We got into an unusual signal generator state! psts:{psts}");
                Thread.Sleep(500);
                break;
            }

            // signal generator needs a little time to start
            Thread.Sleep(50);
        }

        public void ClearBuffer()
        {
            samplesBuffer.Clear();
        }

        public float GetSignalGeneratorFrequency()
        {
            double frequency = 0;

            // calculate the signal generator channel id
            byte signalGeneratorChannelIndex = 255;
            if (this.SignalGeneratorChannel == 1)
            {
                signalGeneratorChannelIndex = 0;
            }
            else if (this.SignalGeneratorChannel == 2)
            {
                signalGeneratorChannelIndex = 1;
            }

            dwf.FDwfAnalogOutFrequencyGet(dwfHandle, signalGeneratorChannelIndex, out frequency);

            return (float)frequency;
        }
    }
}
