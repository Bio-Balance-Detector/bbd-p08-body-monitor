using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.API.Controllers
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
        [Route("frequencyresponseanalysis")]
        public int FrequencyResponseAnalysis()
        {
            int taskId = Task.Run(() =>
            {
                _logger.LogInformation($"Starting frequency analysis.");
                _helpers.FrequencyResponseAnalysis();
            }).Id;

            return taskId;
        }
    }
}