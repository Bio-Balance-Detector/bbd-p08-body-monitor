using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FrequencyResponseController : ControllerBase
    {
        private readonly ILogger<FrequencyResponseController> _logger;
        private readonly IDataProcessorService _helpers;

        public FrequencyResponseController(ILogger<FrequencyResponseController> logger, IDataProcessorService helpers)
        {
            _logger = logger;
            _helpers = helpers;
        }

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