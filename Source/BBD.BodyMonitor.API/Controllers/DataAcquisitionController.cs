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
        [Route("start/{deviceSerialNumber?}/{locationAlias?}/{subjectAlias?}")]
        public string Start(string? deviceSerialNumber = null, string? locationAlias = null, string? subjectAlias = null)
        {
            if (string.IsNullOrEmpty(deviceSerialNumber))
            {
                deviceSerialNumber = _dataProcessor.GetConfig().DeviceSerialNumber;
            }

            if (string.IsNullOrEmpty(locationAlias))
            {
                locationAlias = _dataProcessor.GetConfig().LocationAlias;
            }

            if (string.IsNullOrEmpty(subjectAlias))
            {
                subjectAlias = _dataProcessor.GetConfig().SubjectAlias;
            }

            Sessions.Session session = _sessionManager.StartSession(locationAlias, subjectAlias);
            session.Configuration = _dataProcessor.GetConfig();
            //_sessionManager.SaveSession(session);

            int taskId = Task.Run(() =>
            {
                session.DeviceIdentifier = _dataProcessor.StartDataAcquisition(deviceSerialNumber);
            }).Id;

            return session.Alias;
        }

        [HttpGet]
        [Route("stop/{sessionAlias?}")]
        public void Stop(string? sessionAlias = null)
        {
            if (_sessionManager == null)
            {
                return;
            }

            // find the right session to stop
            IEnumerable<Sessions.Session> runningSessions = _sessionManager.ListSessions().Where(s => s.StartedAt != null && s.FinishedAt == null);
            Sessions.Session? session = runningSessions.FirstOrDefault(s => s.Alias == sessionAlias);
            session ??= runningSessions.First();

            _ = _dataProcessor.StopDataAcquisition(session.DeviceIdentifier);

            session = _sessionManager.FinishSession(session);
            _sessionManager.SaveSession(session);
        }
    }
}