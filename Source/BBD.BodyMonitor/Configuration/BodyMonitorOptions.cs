using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    public class BodyMonitorOptions
    {
        public string DataDirectory { get; } = String.Empty;
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public long MinimumAvailableFreeSpace { get; } = 0;
        public AcqusitionOptions Acquisition { get; }
        public SignalGeneratorOptions SignalGenerator { get; set; }
        public DataWriterOptions DataWriter { get; }
        public PostprocessingOptions Postprocessing { get; }
        public AudioRecordingOptions AudioRecording { get; }
        public IndicatorsOptions Indicators { get; }
        public MachineLearningOptions MachineLearning { get; }
        public FitbitOptions Fitbit { get; }
        public ThingSpeakOptions ThingSpeak { get; }
        public BodyMonitorOptions() { }
        public BodyMonitorOptions(IConfigurationSection config)
        {
            DataDirectory = config["DataDirectory"];
            MinimumAvailableFreeSpace = (long)ParseNumber(config["MinimumAvailableFreeSpace"]);

            Acquisition = new AcqusitionOptions()
            {
                Enabled = Boolean.Parse(config["Acquisition:Enabled"]),
                Channels = config.GetSection("Acquisition:Channels").Get<string[]>().Select(ch => Int32.Parse(new String(ch.TakeLast(1).ToArray()))).ToArray(),
                Buffer = ParseNumber(config["Acquisition:Buffer"]),
                Block = ParseNumber(config["Acquisition:Block"]),
                Samplerate = (int)ParseNumber(config["Acquisition:Samplerate"])
            };

            SignalGenerator = new SignalGeneratorOptions()
            {
                Enabled = Boolean.Parse(config["SignalGenerator:Enabled"]),
                Channel = Byte.Parse(new String(config["SignalGenerator:Channel"].TakeLast(1).ToArray())),
                Frequency = ParseNumber(config["SignalGenerator:Frequency"]),
                Voltage = ParseNumber(config["SignalGenerator:Voltage"]),
            };

            DataWriter = new DataWriterOptions()
            {
                Enabled = Boolean.Parse(config["DataWriter:Enabled"]),
                Interval = ParseNumber(config["DataWriter:Interval"]),
                OutputRange = ParseNumber(config["DataWriter:OutputRange"]),
                SaveAsWAV = Boolean.Parse(config["DataWriter:SaveAsWAV"]),
                SingleFile = Boolean.Parse(config["DataWriter:SingleFile"]),
            };

            Postprocessing = new PostprocessingOptions()
            {
                Enabled = Boolean.Parse(config["Postprocessing:Enabled"]),
                Interval = ParseNumber(config["Postprocessing:Interval"]),
                DataBlock = ParseNumber(config["Postprocessing:DataBlock"]),
                FFTSize = (int)ParseNumber(config["Postprocessing:FFTSize"]),
                MagnitudeThreshold = ParseNumber(config["Postprocessing:MagnitudeThreshold"]),
                ResampleFFTResolutionToHz = ParseNumber(config["Postprocessing:ResampleFFTResolutionToHz"]),
                SaveAsFFT = Boolean.Parse(config["Postprocessing:SaveAsFFT"]),
                SaveAsCompressedFFT = Boolean.Parse(config["Postprocessing:SaveAsCompressedFFT"]),
                SaveAsBinaryFFT = Boolean.Parse(config["Postprocessing:SaveAsBinaryFFT"]),
                SaveAsPNG = new SaveAsPngOptions()
                {
                    Enabled = Boolean.Parse(config["Postprocessing:SaveAsPNG:Enabled"]),
                    TargetWidth = Int32.Parse(config["Postprocessing:SaveAsPNG:TargetWidth"]),
                    TargetHeight = Int32.Parse(config["Postprocessing:SaveAsPNG:TargetHeight"]),
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
                Enabled = Boolean.Parse(config["AudioRecording:Enabled"]),
                Interval = ParseNumber(config["AudioRecording:Interval"]),
                PreferredDevice = config["AudioRecording:PreferredDevice"],
                SilenceThreshold = ParseNumber(config["AudioRecording:SilenceThreshold"])
            };

            Indicators = new IndicatorsOptions()
            {
                Enabled = Boolean.Parse(config["Indicators:Enabled"]),
                Interval = ParseNumber(config["Indicators:Interval"]),
                ModelsToUse = config.GetSection("Indicators:ModelsToUse").Get<string[]>()
            };

            MachineLearning = new MachineLearningOptions()
            {
                Profiles = config.GetSection("MachineLearning:Profiles").Get<MLProfile[]>(),
                CSVHeaders = Boolean.Parse(config["MachineLearning:CSVHeaders"]),
                GenerateMBConfigs = Boolean.Parse(config["MachineLearning:GenerateMBConfigs"])
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
                APIKey = config["ThingSpeak:APIKey"],
                Channel = (int)ParseNumber(config["ThingSpeak:Channel"])
            };
        }

        [Obsolete]
        private float ParseNumber(string str)
        {
            string[] postfixes = { "p", "n", "u", "m", "", "k", "M", "T", "P" };
            bool binaryMode = !str.Contains(" ");

            int numberIndex = 1;
            float numberPart = 0;
            while ((numberIndex <= str.Length) && (Single.TryParse(str.Substring(0, numberIndex), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out float parsedNumberPart)))
            {
                numberIndex++;
                numberPart = parsedNumberPart;
            }

            string textPart = str.Substring(numberIndex - 1).Trim();

            if (textPart == "%")
            {
                numberPart /= 100;
            }
            else
            {
                int postfixIndex = 4;
                for (int i = 0; i < postfixes.Length; i++)
                {
                    if ((postfixes[i] != "") && (textPart.StartsWith(postfixes[i])))
                    {
                        postfixIndex = i;
                        break;
                    }
                }

                while (postfixIndex < 4)
                {
                    numberPart /= (binaryMode ? 1024 : 1000);
                    postfixIndex++;
                }

                while (postfixIndex > 4)
                {
                    numberPart *= (binaryMode ? 1024 : 1000);
                    postfixIndex--;
                }
            }

            return numberPart;
        }
    }
}