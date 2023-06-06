using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostprocessorController : ControllerBase
    {
        private readonly ILogger<PostprocessorController> _logger;
        private readonly IDataProcessorService _dataProcessor;

        public PostprocessorController(ILogger<PostprocessorController> logger, IDataProcessorService dataProcessor)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
        }

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