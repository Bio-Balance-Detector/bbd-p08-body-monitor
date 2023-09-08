using BBD.BodyMonitor.Indicators;
using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;

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
                session.DeviceIdentifier = _dataProcessor.StartDataAcquisition(deviceSerialNumber, session);
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


        // Add a streaming endpoint for evaluated indicators
        [HttpGet]
        [Route("streamindicators")]
        public async Task StreamIndicators()
        {
            HttpResponse response = Response;
            CancellationToken cancellationToken = response.HttpContext.RequestAborted;

            if (!response.HttpContext.WebSockets.IsWebSocketRequest)
            {
                response.StatusCode = 400;
                return;
            }

            WebSocket webSocket = await response.HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    IndicatorEvaluationResult[]? indicatorResults = _dataProcessor.GetLatestIndicatorResults();

                    if (indicatorResults != null)
                    {
                        string json = System.Text.Json.JsonSerializer.Serialize(indicatorResults);
                        await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancellationToken);
                        _logger.LogTrace($"Sent {json.Length} bytes to client on the websocket channel");
                    }
                    await Task.Delay(100, cancellationToken);
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "DisposedWebSocket", cancellationToken);
                }
            }
        }

    }
}