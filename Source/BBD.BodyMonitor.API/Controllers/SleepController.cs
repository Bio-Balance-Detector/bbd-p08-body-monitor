using BBD.BodyMonitor.Controllers;
using Fitbit.Api.Portable.Models;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SleepController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IConfiguration _config;

        public SleepController(ILogger<SystemController> logger, IConfiguration configRoot)
        {
            _logger = logger;
            _config = configRoot;
        }

        [HttpGet]
        [Route("getsleepstages/{date}")]
        public LevelsData[] GetSleepStages(DateTime date)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route("getheartrate/{date}")]
        public DatasetInterval[] GetHeartRate(DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}