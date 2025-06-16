using Microsoft.Extensions.Configuration;
using System.ComponentModel;

namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Represents the main configuration options for the BodyMonitor application.
    /// </summary>
    public class BodyMonitorOptions
    {
        /// <summary>
        /// Reference to the IConfigurationSection used by the obsolete constructor.
        /// </summary>
        private readonly IConfigurationSection? _configRoot;
        /// <summary>
        /// Gets or sets the directory where data will be stored.
        /// </summary>
        public string DataDirectory { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the minimum available free space required on the data storage drive, specified as a string with a unit (e.g., "1GB", "500MB").
        /// </summary>
        [TypeConverter(typeof(StringWithUnitToNumberConverter))]
        public long MinimumAvailableFreeSpace { get; set; } = 0;
        /// <summary>
        /// Gets or sets the serial number of the acquisition device.
        /// </summary>
        public string DeviceSerialNumber { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets an alias for the location where the monitoring is taking place.
        /// </summary>
        public string LocationAlias { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets an alias for the subject being monitored.
        /// </summary>
        public string SubjectAlias { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the data acquisition options.
        /// </summary>
        public AcqusitionOptions Acquisition { get; set; } = new();
        /// <summary>
        /// Gets or sets the signal generator options.
        /// </summary>
        public SignalGeneratorOptions SignalGenerator { get; set; }
        /// <summary>
        /// Gets or sets the data writer options.
        /// </summary>
        public DataWriterOptions DataWriter { get; set; } = new();
        /// <summary>
        /// Gets or sets the postprocessing options.
        /// </summary>
        public PostprocessingOptions Postprocessing { get; set; } = new();
        /// <summary>
        /// Gets or sets the audio recording options.
        /// </summary>
        public AudioRecordingOptions AudioRecording { get; set; } = new();
        /// <summary>
        /// Gets or sets the indicators options.
        /// </summary>
        public IndicatorsOptions Indicators { get; set; } = new();
        /// <summary>
        /// Gets or sets the machine learning options.
        /// </summary>
        public MachineLearningOptions MachineLearning { get; set; } = new();
        /// <summary>
        /// Gets or sets the Fitbit integration options.
        /// </summary>
        public FitbitOptions Fitbit { get; set; }
        /// <summary>
        /// Gets or sets the ThingSpeak integration options.
        /// </summary>
        public ThingSpeakOptions ThingSpeak { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMonitorOptions"/> class. This constructor is used by the modern configuration binding system.
        /// </summary>
        public BodyMonitorOptions() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BodyMonitorOptions"/> class using an <see cref="IConfigurationSection"/>.
        /// This constructor is obsolete and primarily used for compatibility with older configuration methods.
        /// Modern applications should rely on the default constructor and configuration binding.
        /// </summary>
        /// <param name="config">The configuration section to load options from.</param>
        [Obsolete("This constructor is intended for use with older configuration methods. Prefer using IOptions pattern with binding.")]
        public BodyMonitorOptions(IConfigurationSection config)
        {
            _configRoot = config;
            DataDirectory = config["DataDirectory"] ?? string.Empty;
            MinimumAvailableFreeSpace = (long)ParseNumber(config["MinimumAvailableFreeSpace"] ?? "0");
            DeviceSerialNumber = config["DeviceSerialNumber"] ?? string.Empty;
            LocationAlias = config["LocationAlias"] ?? string.Empty;
            SubjectAlias = config["SubjectAlias"] ?? string.Empty;

            Acquisition = new AcqusitionOptions()
            {
                Enabled = bool.Parse(config["Acquisition:Enabled"] ?? "false"),
                Channels = config.GetSection("Acquisition:Channels").Get<string[]>() ?? Array.Empty<string>(),
                Buffer = ParseNumber(config["Acquisition:Buffer"] ?? "0"),
                Block = ParseNumber(config["Acquisition:Block"] ?? "0"),
                Samplerate = (int)ParseNumber(config["Acquisition:Samplerate"] ?? "0")
            };

            ParseSignalGeneratorParameters();

            DataWriter = new DataWriterOptions()
            {
                Enabled = bool.Parse(config["DataWriter:Enabled"] ?? "false"),
                Interval = ParseNumber(config["DataWriter:Interval"] ?? "0"),
                OutputRange = ParseNumber(config["DataWriter:OutputRange"] ?? "0"),
                SaveAsWAV = bool.Parse(config["DataWriter:SaveAsWAV"] ?? "false"),
                SingleFile = bool.Parse(config["DataWriter:SingleFile"] ?? "false"),
            };

            Postprocessing = new PostprocessingOptions()
            {
                Enabled = bool.Parse(config["Postprocessing:Enabled"] ?? "false"),
                Interval = ParseNumber(config["Postprocessing:Interval"] ?? "0"),
                DataBlock = ParseNumber(config["Postprocessing:DataBlock"] ?? "0"),
                FFTSize = (int)ParseNumber(config["Postprocessing:FFTSize"] ?? "0"),
                MagnitudeThreshold = ParseNumber(config["Postprocessing:MagnitudeThreshold"] ?? "0"),
                ResampleFFTResolutionToHz = ParseNumber(config["Postprocessing:ResampleFFTResolutionToHz"] ?? "0"),
                SaveAsFFT = bool.Parse(config["Postprocessing:SaveAsFFT"] ?? "false"),
                SaveAsCompressedFFT = bool.Parse(config["Postprocessing:SaveAsCompressedFFT"] ?? "false"),
                SaveAsBinaryFFT = bool.Parse(config["Postprocessing:SaveAsBinaryFFT"] ?? "false"),
                SaveAsPNG = new SaveAsPngOptions()
                {
                    Enabled = bool.Parse(config["Postprocessing:SaveAsPNG:Enabled"] ?? "false"),
                    TargetWidth = int.Parse(config["Postprocessing:SaveAsPNG:TargetWidth"] ?? "0"),
                    TargetHeight = int.Parse(config["Postprocessing:SaveAsPNG:TargetHeight"] ?? "0"),
                    RangeX = (int)ParseNumber(config["Postprocessing:SaveAsPNG:RangeX"] ?? "0"),
                    RangeY = new SaveAsPngRangeOptions()
                    {
                        Min = ParseNumber(config["Postprocessing:SaveAsPNG:RangeY:Min"] ?? "0"),
                        Max = ParseNumber(config["Postprocessing:SaveAsPNG:RangeY:Max"] ?? "0"),
                        Unit = config["Postprocessing:SaveAsPNG:RangeY:Unit"] ?? string.Empty,
                        Format = config["Postprocessing:SaveAsPNG:RangeY:Format"] ?? string.Empty,
                    },
                    MLProfile = config["Postprocessing:SaveAsPNG:MLProfile"] ?? string.Empty
                },
            };

            AudioRecording = new AudioRecordingOptions()
            {
                Enabled = bool.Parse(config["AudioRecording:Enabled"] ?? "false"),
                Interval = ParseNumber(config["AudioRecording:Interval"] ?? "0"),
                PreferredDevice = config["AudioRecording:PreferredDevice"] ?? string.Empty,
                SilenceThreshold = ParseNumber(config["AudioRecording:SilenceThreshold"] ?? "0")
            };

            Indicators = new IndicatorsOptions()
            {
                Enabled = bool.Parse(config["Indicators:Enabled"] ?? "false"),
                Interval = ParseNumber(config["Indicators:Interval"] ?? "0"),
                AverageOf = (int)ParseNumber(config["Indicators:AverageOf"] ?? "0"),
                ModelsToUse = config.GetSection("Indicators:ModelsToUse").Get<string[]>() ?? Array.Empty<string>()
            };

            MachineLearning = new MachineLearningOptions()
            {
                Profiles = config.GetSection("MachineLearning:Profiles").Get<MLProfile[]>() ?? Array.Empty<MLProfile>(),
                CSVHeaders = bool.Parse(config["MachineLearning:CSVHeaders"] ?? "false"),
                GenerateMBConfigs = bool.Parse(config["MachineLearning:GenerateMBConfigs"] ?? "false")
            };

            Fitbit = new FitbitOptions()
            {
                RedirectURL = new Uri(config["Fitbit:RedirectURL"] ?? "http://localhost/"),
                ClientID = config["Fitbit:ClientID"] ?? string.Empty,
                ClientSecret = config["Fitbit:ClientSecret"] ?? string.Empty
            };

            ThingSpeak = new ThingSpeakOptions()
            {
                APIEndpoint = new Uri(config["ThingSpeak:APIEndpoint"] ?? "http://localhost/"),
                APIKey = config["ThingSpeak:APIKey"] ?? string.Empty
            };
        }

        /// <summary>
        /// Parses the signal generator parameters from the configuration.
        /// This method is typically called by the obsolete constructor.
        /// </summary>
        public void ParseSignalGeneratorParameters()
        {
            if (_configRoot == null) return;

            string[]? scheduleStrings = _configRoot.GetSection("SignalGenerator:Schedule").Get<string[]>();
            ScheduleOptions[] schedules = scheduleStrings == null ? Array.Empty<ScheduleOptions>() : scheduleStrings.Select(ScheduleOptions.Parse).ToArray();

            SignalGenerator = new SignalGeneratorOptions()
            {
                SignalDefinitions = _configRoot.GetSection("SignalGenerator:SignalDefinitions").Get<SignalDefinitionOptions[]>() ?? Array.Empty<SignalDefinitionOptions>(),
                Schedules = schedules
            };

            foreach (SignalDefinitionOptions signalDefinition in SignalGenerator.SignalDefinitions)
            {
                signalDefinition.ParseFrequency();
                signalDefinition.ParseAmplitude();
            }
        }

        /// <summary>
        /// Parses a string representation of a number with an optional unit postfix (e.g., "10k", "2M", "500m").
        /// </summary>
        /// <param name="str">The string to parse.</param>
        /// <returns>The parsed float value.</returns>
        [Obsolete("Use StringWithUnitToNumberConverter.ConvertFrom instead")]
        private float ParseNumber(string str)
        {
            string[] postfixes = { "p", "n", "u", "m", "", "k", "M", "T", "P" }; // p to P, including empty for no unit
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