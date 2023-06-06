using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace BBD.BodyMonitor
{
    public class SamplesReceivedEventArgs
    {
        public SamplesReceivedEventArgs(float[] data, int bufferIndex, bool bufferError)
        {
            Samples = data; BufferIndex = bufferIndex;
            BufferError = bufferError;
        }
        public float[] Samples { get; }
        public int BufferIndex { get; }
        public bool BufferError { get; }
    }

    internal class DataAcquisition
    {
        public float Samplerate { get; private set; }
        public int MinimumSamplesToReceive { get; internal set; }
        public bool SignalGeneratorEnabled { get; private set; }
        public byte SignalGeneratorChannel { get; private set; }
        public float SignalGeneratorFrequency { get; private set; }
        public float SignalGeneratorVoltage { get; private set; }

        public delegate void SamplesReceivedEventHandler(object sender, SamplesReceivedEventArgs e);

        public event SamplesReceivedEventHandler SamplesReceived;

        private ILogger logger;

        private int dwfHandle = -1;

        /// <summary>
        /// Number of samples per buffer
        /// </summary>
        private int bufferSize;

        private float[] samples;
        private int samplesCount;
        private double[] voltData;

        private bool terminateAcquisition = false;

        public DataAcquisition(ILogger logger)
        {
            this.logger = logger;

            dwf.FDwfGetVersion(out string dwfVersion);
            logger.LogInformation($"DWF Version: {dwfVersion}");
        }

        public int OpenDevice(float samplerate, bool signalGeneratorEnabled, byte signalGeneratorChannel, float signalGeneratorFrequency, float signalGeneratorVoltage)
        {
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
            bufferSize = Math.Min(bufferSizeMaximum, (int)samplerate);
            logger.LogTrace($"Device buffer size range: {bufferSizeMinimum:N0} - {bufferSizeMaximum:N0} samples, set to {bufferSize:N0}.");
            voltData = new double[bufferSize];

            //set up acquisition
            dwf.FDwfAnalogInFrequencySet(dwfHandle, samplerate);
            dwf.FDwfAnalogInFrequencyGet(dwfHandle, out double realSamplerate);

            this.Samplerate = (float)realSamplerate;

            if (samplerate != (int)realSamplerate)
            {
                logger.LogWarning($"The sampling rate of {samplerate:N} Hz is not supported, so the effective sampling rate was set to {realSamplerate:N} Hz. Native sampling rates for AD2 are the following: 1, 2, 4, 5, 8, 10, 16, 20, 25, 32, 40, 50, 64, 80, 100, 125, 128, 160, 200, 250, 256, 320, 400, 500, 625, 640, 800 Hz, 1, 1.25, 1.28, 1.6, 2, 2.5, 3.125, 3.2, 4, 5, 6.25, 6.4, 8, 10, 12.5, 15.625, 16, 20, 25, 31.250, 32, 40, 50, 62.5, 78.125, 80, 100, 125, 156.25, 160, 200, 250, 312.5, 390.625, 400, 500, 625, 781.25, 800 kHz, 1, 1.25, 1.5625, 2, 2.5, 3.125, 4, 5, 6.25, 10, 12.5, 20, 25, 50 and 100 MHz.");
            }

            dwf.FDwfAnalogInBufferSizeSet(dwfHandle, bufferSize);
            dwf.FDwfAnalogInChannelEnableSet(dwfHandle, 0, 1);
            dwf.FDwfAnalogInChannelRangeSet(dwfHandle, 0, 5.0);
            dwf.FDwfAnalogInAcquisitionModeSet(dwfHandle, dwf.acqmodeRecord);
            dwf.FDwfAnalogInRecordLengthSet(dwfHandle, -1);

            if (signalGeneratorEnabled)
            {
                // set up signal generation
                dwf.FDwfAnalogOutNodeEnableSet(dwfHandle, signalGeneratorChannel, dwf.AnalogOutNodeCarrier, 1);
                dwf.FDwfAnalogOutNodeFunctionSet(dwfHandle, signalGeneratorChannel, dwf.AnalogOutNodeCarrier, dwf.funcSine);
                dwf.FDwfAnalogOutNodeFrequencySet(dwfHandle, signalGeneratorChannel, dwf.AnalogOutNodeCarrier, signalGeneratorFrequency);
                dwf.FDwfAnalogOutNodeAmplitudeSet(dwfHandle, signalGeneratorChannel, dwf.AnalogOutNodeCarrier, signalGeneratorVoltage);

                logger.LogTrace($"Generating sine wave at {signalGeneratorFrequency:N} Hz with {signalGeneratorVoltage} V of amplitude at channel {signalGeneratorChannel}...");
                dwf.FDwfAnalogOutConfigure(dwfHandle, signalGeneratorChannel, 1);

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

            return OpenDevice(this.Samplerate, this.SignalGeneratorEnabled, this.SignalGeneratorChannel, this.SignalGeneratorFrequency, this.SignalGeneratorVoltage);
        }

        public void Start()
        {
            terminateAcquisition = false;

            int bufferIndex = 0;
            bool bufferError = false;

            samples = new float[this.MinimumSamplesToReceive * 2];
            samplesCount = 0;

            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            while (!terminateAcquisition)
            {
                while (true)
                {
                    //logger.LogTrace($"FDwfAnalogInStatus begin | dwfHandle:{dwfHandle}");
                    dwf.FDwfAnalogInStatus(dwfHandle, 1, out byte sts);
                    //logger.LogTrace($"FDwfAnalogInStatus end   | sts:{sts}");

                    if (!((samplesCount == 0) && ((sts == dwf.DwfStateConfig) || (sts == dwf.DwfStatePrefill) || (sts == dwf.DwfStateArmed))))
                        break;

                    logger.LogWarning($"We got into an unusual state! sts:{sts}");
                    Thread.Sleep(500);
                }

                //logger.LogTrace($"FDwfAnalogInStatusRecord begin | dwfHandle:{dwfHandle}");
                dwf.FDwfAnalogInStatusRecord(dwfHandle, out int cAvailable, out int cLost, out int cCorrupted);
                //logger.LogTrace($"FDwfAnalogInStatusRecord end   | cAvailable:{cAvailable:N0}, cLost:{cLost:N0}, cCorrupted:{cCorrupted:N0}");

                if (cAvailable == 0)
                {
                    logger.LogWarning($"Aqusition error! cAvailable: {cAvailable:N0}");
                    Thread.Sleep(500);

                    logger.LogTrace($"Reseting device...");
                    dwfHandle = ResetDevice();

                    bufferError = false;
                    samplesCount = 0;
                    continue;
                }

                if ((cLost > 0) || (cCorrupted > 0))
                {
                    logger.LogWarning($"Data error! cLost:{cLost:N0}, cCorrupted:{cCorrupted:N0}");
                    bufferError = true;
                }

                //logger.LogTrace($"FDwfAnalogInStatusData begin | dwfHandle:{dwfHandle}, cAvailable:{cAvailable:N0}");
                dwf.FDwfAnalogInStatusData(dwfHandle, 0, voltData, cAvailable);     // get channel 1 data chunk
                //logger.LogTrace($"FDwfAnalogInStatusData end   | voltData.Count:{voltData.Count():N0}");

                double[] voltDataAvailable = new double[cAvailable];
                Array.Copy(voltData, voltDataAvailable, cAvailable);
                float[] voltDataAvailableFloat = Array.ConvertAll<double, float>(voltDataAvailable, v => (float)v);
                Array.Copy(voltDataAvailableFloat, 0, samples, samplesCount, voltDataAvailableFloat.Length);
                samplesCount += voltDataAvailableFloat.Length;

                if ((samplesCount > 0) && (samplesCount >= this.MinimumSamplesToReceive))
                {
                    float[] samplesToSend = samples[0..this.MinimumSamplesToReceive];
                    float[] samplesRemained = samples[this.MinimumSamplesToReceive..samplesCount];

                    SamplesReceived?.Invoke(this, new SamplesReceivedEventArgs(samplesToSend, bufferIndex++, bufferError));

                    bufferError = false;
                    samplesCount = 0;
                    if (samplesRemained.Length > 0)
                    {
                        Array.Copy(samplesRemained, 0, samples, samplesCount, samplesRemained.Length);
                        samplesCount += samplesRemained.Length;
                    }
                }
            }

            samplesCount = 0;
            logger.LogTrace("Acquisition done");
        }

        public void Stop()
        {
            terminateAcquisition = true;
        }
    }
}
