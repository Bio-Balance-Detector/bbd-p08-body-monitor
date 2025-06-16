using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.Controllers
{
    /// <summary>
    /// Controller for performing frequency response analysis.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class FrequencyResponseController : ControllerBase
    {
        private readonly ILogger<FrequencyResponseController> _logger;
        private readonly IDataProcessorService _helpers;

        /// <summary>
        /// Initializes a new instance of the <see cref="FrequencyResponseController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="helpers">The data processor service, used here for its helper functions related to frequency analysis.</param>
        public FrequencyResponseController(ILogger<FrequencyResponseController> logger, IDataProcessorService helpers)
        {
            _logger = logger;
            _helpers = helpers;
        }

        /// <summary>
        /// Initiates a frequency response analysis for a specified device.
        /// </summary>
        /// <remarks>
        /// This method starts the frequency response analysis as a background task.
        /// The analysis is performed by the <see cref="IDataProcessorService.FrequencyResponseAnalysis"/> method.
        /// </remarks>
        /// <param name="deviceSerialNumber">The serial number of the device to analyze.</param>
        /// <returns>The ID of the background task performing the analysis.</returns>
        [HttpGet]
        [Route("frequencyresponseanalysis/{deviceSerialNumber}")]
        public int FrequencyResponseAnalysis(string deviceSerialNumber)
        {
            int taskId = Task.Run(() =>
            {
                _logger.LogInformation($"Starting frequency analysis.");
                _helpers.FrequencyResponseAnalysis(deviceSerialNumber);
            }).Id;

            return taskId;
        }
    }
}