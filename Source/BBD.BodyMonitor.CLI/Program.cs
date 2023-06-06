using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace BBD.BodyMonitor.CLI
{
    class Program
    {
        static string versionString = "v0.8.3";

        static ILogger logger;

        static Mutex mutex = new Mutex(true, "{79bb7f72-37bc-41ff-9014-ed8662659b52}");

        static IConfigurationRoot configuration;
        static BodyMonitorConfig config;

        static Helpers helpers;

        static void Main(string[] args)
        {
            var argsMappings = new Dictionary<string, string>
            {
                { "--datadirectory", "DataDirectory" },
                { "--samplerate", "AD2:Samplerate" }
            };

            // Build configuration
            configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>(optional: true)
            .AddCommandLine(args, argsMappings)
            .Build();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", Enum.Parse<LogLevel>(configuration["Logging:LogLevel:Default"]))
                    .AddFilter("System", Enum.Parse<LogLevel>(configuration["Logging:LogLevel:Default"]))
                    .AddFilter("BBD.BodyMonitor.Program", Enum.Parse<LogLevel>(configuration["Logging:LogLevel:BBD.BodyMonitor.Program"]))
                    .AddConfiguration(configuration)
                    .AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = false;
                        options.SingleLine = true;
                        options.TimestampFormat = "HH:mm:ss.ffff ";
                        options.UseUtcTimestamp = false;
                    })
                    .AddDebug();
            });
            logger = loggerFactory.CreateLogger<Program>();

            Console.CancelKeyPress += Console_CancelKeyPress;

            Helpers.ShowWelcomeScreen(versionString);

            logger.LogInformation($"Bio Balance Detector Body Monitor {versionString}");
            logger.LogInformation($"(The current UTC time is {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss})");
            try
            {
                configuration.Reload();
                config = new BodyMonitorConfig(configuration);
                //configuration.GetSection("MachineLearning").Bind(config.MachineLearning);
            }
            catch (Exception ex)
            {
                logger.LogError($"There was a problem with the configuration file. {ex.Message}");
                return;
            }

            if (!mutex.WaitOne(TimeSpan.Zero, true))
            {
                logger.LogError($"An instance of this app is already running on this machine.");
                return;
            }

            helpers = new Helpers(logger, config);

            helpers.GetFFMPEGAudioDevices();

            helpers.ProcessCommandLineArguments(args);
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            helpers.StopDataAcquisition();
            e.Cancel = true;

            mutex.ReleaseMutex();
        }
    }
}
