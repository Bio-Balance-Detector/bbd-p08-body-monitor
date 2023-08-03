using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class BodyMonitorOptions
    {
        public string DataDirectory { get; set; } = string.Empty;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public long MinimumAvailableFreeSpace { get; set; } = 0;
        public string DeviceSerialNumber { get; set; } = string.Empty;
        public string LocationAlias { get; set; } = string.Empty;
        public string SubjectAlias { get; set; } = string.Empty;
        public AcqusitionOptions Acquisition { get; set; }
        public SignalGeneratorOptions SignalGenerator { get; set; }
        public DataWriterOptions DataWriter { get; set; }
        public PostprocessingOptions Postprocessing { get; set; }
        public AudioRecordingOptions AudioRecording { get; set; }
        public IndicatorsOptions Indicators { get; set; }
        public MachineLearningOptions MachineLearning { get; set; }
        public FitbitOptions Fitbit { get; set; }
        public ThingSpeakOptions ThingSpeak { get; set; }
        public BodyMonitorOptions() { }

        [Obsolete]
        public BodyMonitorOptions(IConfigurationSection config)
        {
            DataDirectory = config["DataDirectory"];
            MinimumAvailableFreeSpace = (long)ParseNumber(config["MinimumAvailableFreeSpace"]);
            DeviceSerialNumber = config["DeviceSerialNumber"];
            LocationAlias = config["LocationAlias"];
            SubjectAlias = config["SubjectAlias"];

            Acquisition = new AcqusitionOptions()
            {
                Enabled = bool.Parse(config["Acquisition:Enabled"]),
                Channels = config.GetSection("Acquisition:Channels").Get<string[]>().Select(ch => int.Parse(new string(ch.TakeLast(1).ToArray()))).ToArray(),
                Buffer = ParseNumber(config["Acquisition:Buffer"]),
                Block = ParseNumber(config["Acquisition:Block"]),
                Samplerate = (int)ParseNumber(config["Acquisition:Samplerate"])
            };

            string[]? scheduleStrings = config.GetSection("SignalGenerator:Schedule").Get<string[]>();
            ScheduleOptions[] schedules = scheduleStrings.Select(ScheduleOptions.Parse).ToArray();

            SignalGenerator = new SignalGeneratorOptions()
            {
                SignalDefinitions = config.GetSection("SignalGenerator:SignalDefinitions").Get<SignalDefinitionOptions[]>(),
                Schedules = schedules
            };

            foreach (SignalDefinitionOptions signalDefinition in SignalGenerator.SignalDefinitions)
            {
                signalDefinition.ParseFrequency();
                signalDefinition.ParseAmplitude();
            }

            DataWriter = new DataWriterOptions()
            {
                Enabled = bool.Parse(config["DataWriter:Enabled"]),
                Interval = ParseNumber(config["DataWriter:Interval"]),
                OutputRange = ParseNumber(config["DataWriter:OutputRange"]),
                SaveAsWAV = bool.Parse(config["DataWriter:SaveAsWAV"]),
                SingleFile = bool.Parse(config["DataWriter:SingleFile"]),
            };

            Postprocessing = new PostprocessingOptions()
            {
                Enabled = bool.Parse(config["Postprocessing:Enabled"]),
                Interval = ParseNumber(config["Postprocessing:Interval"]),
                DataBlock = ParseNumber(config["Postprocessing:DataBlock"]),
                FFTSize = (int)ParseNumber(config["Postprocessing:FFTSize"]),
                MagnitudeThreshold = ParseNumber(config["Postprocessing:MagnitudeThreshold"]),
                ResampleFFTResolutionToHz = ParseNumber(config["Postprocessing:ResampleFFTResolutionToHz"]),
                SaveAsFFT = bool.Parse(config["Postprocessing:SaveAsFFT"]),
                SaveAsCompressedFFT = bool.Parse(config["Postprocessing:SaveAsCompressedFFT"]),
                SaveAsBinaryFFT = bool.Parse(config["Postprocessing:SaveAsBinaryFFT"]),
                SaveAsPNG = new SaveAsPngOptions()
                {
                    Enabled = bool.Parse(config["Postprocessing:SaveAsPNG:Enabled"]),
                    TargetWidth = int.Parse(config["Postprocessing:SaveAsPNG:TargetWidth"]),
                    TargetHeight = int.Parse(config["Postprocessing:SaveAsPNG:TargetHeight"]),
                    RangeX = (int)ParseNumber(config["Postprocessing:SaveAsPNG:RangeX"]),
                    RangeY = new SaveAsPngRangeOptions()
                    {
                        Min = ParseNumber(config["Postprocessing:SaveAsPNG:RangeY:Min"]),
                        Max = ParseNumber(config["Postprocessing:SaveAsPNG:RangeY:Max"]),
                        Unit = config["Postprocessing:SaveAsPNG:RangeY:Unit"],
                        Format = config["Postprocessing:SaveAsPNG:RangeY:Format"],
                    },
                    MLProfile = config["Postprocessing:SaveAsPNG:MLProfile"]
                },
            };

            AudioRecording = new AudioRecordingOptions()
            {
                Enabled = bool.Parse(config["AudioRecording:Enabled"]),
                Interval = ParseNumber(config["AudioRecording:Interval"]),
                PreferredDevice = config["AudioRecording:PreferredDevice"],
                SilenceThreshold = ParseNumber(config["AudioRecording:SilenceThreshold"])
            };

            Indicators = new IndicatorsOptions()
            {
                Enabled = bool.Parse(config["Indicators:Enabled"]),
                Interval = ParseNumber(config["Indicators:Interval"]),
                ModelsToUse = config.GetSection("Indicators:ModelsToUse").Get<string[]>()
            };

            MachineLearning = new MachineLearningOptions()
            {
                Profiles = config.GetSection("MachineLearning:Profiles").Get<MLProfile[]>(),
                CSVHeaders = bool.Parse(config["MachineLearning:CSVHeaders"]),
                GenerateMBConfigs = bool.Parse(config["MachineLearning:GenerateMBConfigs"])
            };

            Fitbit = new FitbitOptions()
            {
                RedirectURL = new Uri(config["Fitbit:RedirectURL"]),
                ClientID = config["Fitbit:ClientID"],
                ClientSecret = config["Fitbit:ClientSecret"]
            };

            ThingSpeak = new ThingSpeakOptions()
            {
                APIEndpoint = new Uri(config["ThingSpeak:APIEndpoint"]),
                APIKey = config["ThingSpeak:APIKey"]
            };
        }

        [Obsolete("Use StringWithUnitToNumberConverter.ConvertFrom instead")]
        private float ParseNumber(string str)
        {
            string[] postfixes = { "p", "n", "u", "m", "", "k", "M", "T", "P" };
            bool binaryMode = !str.Contains(" ");

            int numberIndex = 1;
            float numberPart = 0;
            while ((numberIndex <= str.Length) && float.TryParse(str[..numberIndex], System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out float parsedNumberPart))
            {
                numberIndex++;
                numberPart = parsedNumberPart;
            }

            string textPart = str[(numberIndex - 1)..].Trim();

            if (textPart == "%")
            {
                numberPart /= 100;
            }
            else
            {
                int postfixIndex = 4;
                for (int i = 0; i < postfixes.Length; i++)
                {
                    if ((postfixes[i] != "") && textPart.StartsWith(postfixes[i]))
                    {
                        postfixIndex = i;
                        break;
                    }
                }

                while (postfixIndex < 4)
                {
                    numberPart /= binaryMode ? 1024 : 1000;
                    postfixIndex++;
                }

                while (postfixIndex > 4)
                {
                    numberPart *= binaryMode ? 1024 : 1000;
                    postfixIndex--;
                }
            }

            return numberPart;
        }
    }
}