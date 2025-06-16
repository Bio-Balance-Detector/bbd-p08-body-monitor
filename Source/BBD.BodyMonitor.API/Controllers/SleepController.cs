using BBD.BodyMonitor.Controllers;
using Fitbit.Api.Portable.Models;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.API.Controllers
{
    /// <summary>
    /// Controller for accessing sleep-related data.
    /// Note: The methods in this controller are currently not implemented.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SleepController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="SleepController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="configRoot">The root configuration interface.</param>
        public SleepController(ILogger<SystemController> logger, IConfiguration configRoot)
        {
            _logger = logger;
            _config = configRoot;
        }

        /// <summary>
        /// Gets sleep stage data for a specified date.
        /// </summary>
        /// <remarks>
        /// This method is currently not implemented and will throw a <see cref="NotImplementedException"/>.
        /// It is intended to return sleep stage information, possibly from a service like Fitbit.
        /// </remarks>
        /// <param name="date">The date for which to retrieve sleep stage data.</param>
        /// <returns>An array of <see cref="LevelsData"/> objects representing sleep stages. Currently throws <see cref="NotImplementedException"/>.</returns>
        /// <response code="501">Indicates that the method is not implemented.</response>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        [HttpGet]
        [Route("getsleepstages/{date}")]
        public LevelsData[] GetSleepStages(DateTime date)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets heart rate data for a specified date.
        /// </summary>
        /// <remarks>
        /// This method is currently not implemented and will throw a <see cref="NotImplementedException"/>.
        /// It is intended to return heart rate information, possibly from a service like Fitbit, typically recorded during sleep.
        /// </remarks>
        /// <param name="date">The date for which to retrieve heart rate data.</param>
        /// <returns>An array of <see cref="DatasetInterval"/> objects representing heart rate measurements. Currently throws <see cref="NotImplementedException"/>.</returns>
        /// <response code="501">Indicates that the method is not implemented.</response>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        [HttpGet]
        [Route("getheartrate/{date}")]
        public DatasetInterval[] GetHeartRate(DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}