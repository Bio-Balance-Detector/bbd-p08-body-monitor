using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.API.Controllers
{
    /// <summary>
    /// Controller for handling post-processing tasks such as generating FFT data, videos, and EDF files from acquired data.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class PostprocessorController : ControllerBase
    {
        private readonly ILogger<PostprocessorController> _logger;
        private readonly IDataProcessorService _dataProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostprocessorController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="dataProcessor">The data processor service for handling data operations.</param>
        public PostprocessorController(ILogger<PostprocessorController> logger, IDataProcessorService dataProcessor)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
        }

        /// <summary>
        /// Generates Fast Fourier Transform (FFT) data from the session files found in the specified data folder.
        /// </summary>
        /// <remarks>
        /// This operation is performed as a background task.
        /// It uses a machine learning profile to define how data is processed.
        /// </remarks>
        /// <param name="dataFoldername">The name of the folder containing the session data. This folder is typically located within the configured data directory.</param>
        /// <param name="mlProfileName">The beginning of the machine learning profile name (e.g., "ML14") to be used for processing.</param>
        /// <param name="interval">The interval in seconds for FFT calculations.</param>
        /// <returns>The ID of the background task performing the FFT generation. Returns task ID even if the profile is not found, but logs a warning.</returns>
        [HttpGet]
        [Route("generatefft/{dataFoldername}/{mlProfileName}/{interval}")]
        public int GenerateFFT(string dataFoldername, string mlProfileName, float interval)
        {
            int taskId = Task.Run(() =>
            {
                BodyMonitorOptions config = _dataProcessor.GetConfig();

                MLProfile? mlProfile = null;
                mlProfile = config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(mlProfileName));

                if (mlProfile == null)
                {
                    _logger.LogWarning($"The profile '{mlProfileName}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                    return;
                }

                _logger.LogInformation($"Generating FFT files based on data found in the '{dataFoldername}' folder and the machine learning profile '{mlProfile.Name}'.");
                _dataProcessor.GenerateFFT(dataFoldername, mlProfile, interval);
            }).Id;

            return taskId;
        }

        /// <summary>
        /// Generates a video file from the session data found in the specified data folder.
        /// </summary>
        /// <remarks>
        /// This operation is performed as a background task.
        /// It uses a machine learning profile to define how data is visualized.
        /// </remarks>
        /// <param name="dataFoldername">The name of the folder containing the session data. This folder is typically located within the configured data directory.</param>
        /// <param name="mlProfileName">The beginning of the machine learning profile name (e.g., "ML14") to be used for visualization.</param>
        /// <param name="framerate">The frame rate of the generated video.</param>
        /// <returns>The ID of the background task performing the video generation. Returns task ID even if the profile is not found, but logs an error.</returns>
        [HttpGet]
        [Route("generatevideo/{dataFoldername}/{mlProfileName}/{framerate}")]
        public int GenerateVideo(string dataFoldername, string mlProfileName, double framerate)
        {
            int taskId = Task.Run(() =>
            {
                BodyMonitorOptions config = _dataProcessor.GetConfig();

                var mlProfile = config.MachineLearning.Profiles.FirstOrDefault(p => p.Name.StartsWith(mlProfileName));

                if (mlProfile == null)
                {
                    _logger.LogError($"The profile '{mlProfileName}' was not found in the machine learning profile list. Make sure that you have it defined in the appsettings.json file.");
                    return;
                }
                else
                {
                    _logger.LogInformation($"Generating video file based on data found in the '{dataFoldername}' folder and the machine learning profile '{mlProfile.Name}'.");
                    _dataProcessor.GenerateVideo(dataFoldername, mlProfile, framerate);
                }
            }).Id;

            return taskId;
        }

        /// <summary>
        /// Generates an European Data Format (EDF) file from the session data found in the specified data folder within a given time range.
        /// </summary>
        /// <remarks>
        /// This operation is performed as a background task.
        /// EDF is a standard file format for storing multichannel biological and physical signals.
        /// </remarks>
        /// <param name="dataFoldername">The name of the folder containing the session data. This folder is typically located within the configured data directory.</param>
        /// <param name="fromDateTime">The start date and time (inclusive) of the data to include in the EDF file.</param>
        /// <param name="toDateTime">The end date and time (inclusive) of the data to include in the EDF file.</param>
        /// <returns>The ID of the background task performing the EDF file generation.</returns>
        [HttpGet]
        [Route("generateedf/{dataFoldername}/{fromDateTime}/{toDateTime}")]
        public int GenerateEDF(string dataFoldername, DateTimeOffset fromDateTime, DateTimeOffset toDateTime)
        {
            int taskId = Task.Run(() =>
            {
                _logger.LogInformation($"Generating EDF file based on data found in the '{dataFoldername}' folder, between {fromDateTime} and {toDateTime}.");
                _dataProcessor.GenerateEDF(dataFoldername, fromDateTime.UtcDateTime, toDateTime.UtcDateTime);
            }).Id;

            return taskId;
        }

    }
}