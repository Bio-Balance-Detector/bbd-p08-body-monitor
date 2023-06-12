using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;

namespace BBD.BodyMonitor.Controllers
{
    /// <summary>
    /// Controller for data acquisition
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class DataAcquisitionController : ControllerBase
    {
        private readonly ILogger<DataAcquisitionController> _logger;
        private readonly IDataProcessorService _dataProcessor;
        private readonly ISessionManagerService _sessionManager;

        public DataAcquisitionController(ILogger<DataAcquisitionController> logger, IDataProcessorService dataProcessor, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
            _sessionManager = sessionManager;
            _sessionManager.SetDataDirectory(_dataProcessor.GetConfig().DataDirectory);
        }

        [HttpGet]
        [Route("start/{deviceSerialNumber}/{locationAlias}/{subjectAlias}")]
        public int Start(string deviceSerialNumber, string locationAlias, string subjectAlias)
        {
            Sessions.Session session = _sessionManager.StartSession("0x4A75C1F7", "0xBAC08836");
            session.Configuration = _dataProcessor.GetConfig();
            //_sessionManager.SaveSession(session);

            int taskId = Task.Run(() =>
            {
                _ = _dataProcessor.StartDataAcquisition(deviceSerialNumber);
            }).Id;

            return taskId;
        }

        [HttpGet]
        [Route("stop")]
        public void Stop()
        {
            _dataProcessor.StopDataAcquisition();

            Sessions.Session? session = _sessionManager.FinishSession();
            _sessionManager.SaveSession(session);
        }
    }
}