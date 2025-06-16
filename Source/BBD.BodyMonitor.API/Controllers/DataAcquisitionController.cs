using BBD.BodyMonitor.Indicators;
using BBD.BodyMonitor.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;
using System.Text;

namespace BBD.BodyMonitor.Controllers
{
    /// <summary>
    /// Provides endpoints for controlling and monitoring data acquisition processes.
    /// This includes starting and stopping data acquisition sessions, as well as streaming evaluated bio-indicators.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class DataAcquisitionController : ControllerBase
    {
        private readonly ILogger<DataAcquisitionController> _logger;
        private readonly IDataProcessorService _dataProcessor;
        private readonly ISessionManagerService _sessionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataAcquisitionController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="dataProcessor">The data processor service for handling data acquisition and processing.</param>
        /// <param name="sessionManager">The session manager service for managing data acquisition sessions.</param>
        public DataAcquisitionController(ILogger<DataAcquisitionController> logger, IDataProcessorService dataProcessor, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
            _sessionManager = sessionManager;
            _sessionManager.SetDataDirectory(_dataProcessor.GetConfig().DataDirectory);
        }

        /// <summary>
        /// Starts a new data acquisition session.
        /// </summary>
        /// <param name="deviceSerialNumber">Optional. The serial number of the device to use for data acquisition. If not provided, the default device from the configuration will be used.</param>
        /// <param name="locationAlias">Optional. An alias for the location where data acquisition is taking place. If not provided, the default location from the configuration will be used.</param>
        /// <param name="subjectAlias">Optional. An alias for the subject being monitored. If not provided, the default subject from the configuration will be used.</param>
        /// <returns>A string representing the alias of the started session.</returns>
        /// <response code="200">Returns the alias of the successfully started session.</response>
        [HttpGet]
        [Route("start/{deviceSerialNumber?}/{locationAlias?}/{subjectAlias?}")]
        [ProducesResponseType(typeof(string), 200)]
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

        /// <summary>
        /// Stops an ongoing data acquisition session.
        /// </summary>
        /// <param name="sessionAlias">Optional. The alias of the session to stop. If not provided, the most recently started session will be stopped.</param>
        /// <response code="200">Indicates the session was successfully stopped or no session was active.</response>
        [HttpGet]
        [Route("stop/{sessionAlias?}")]
        [ProducesResponseType(200)]
        public void Stop(string? sessionAlias = null)
        {
            if (_sessionManager == null)
            {
                return;
            }

            // find the right session to stop
            IEnumerable<Sessions.Session> runningSessions = _sessionManager.ListSessions().Where(s => s.StartedAt != null && s.FinishedAt == null);
            Sessions.Session? session = runningSessions.FirstOrDefault(s => s.Alias == sessionAlias);
            session ??= runningSessions.FirstOrDefault(); // Ensure we pick the first if no alias matches, or null if no sessions

            if (session != null) // Ensure a session was found before trying to stop
            {
                _ = _dataProcessor.StopDataAcquisition(session.DeviceIdentifier);

                session = _sessionManager.FinishSession(session);
                _sessionManager.SaveSession(session);
            }
        }


        /// <summary>
        /// Establishes a WebSocket connection to stream evaluated bio-indicator results in real-time.
        /// </summary>
        /// <remarks>
        /// This endpoint upgrades the HTTP GET request to a WebSocket connection.
        /// It continuously sends JSON serialized <see cref="IndicatorEvaluationResult"/> arrays to the client.
        /// The stream closes when the client disconnects or the cancellation token is triggered.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of streaming indicator data.</returns>
        /// <response code="101">If the request is upgraded to a WebSocket connection.</response>
        /// <response code="400">If the request is not a WebSocket request.</response>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled, e.g., client disconnects.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs during WebSocket communication.</exception>
        [HttpGet]
        [Route("streamindicators")]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task StreamIndicators()
        {
            HttpResponse response = Response;
            CancellationToken cancellationToken = response.HttpContext.RequestAborted;

            if (!response.HttpContext.WebSockets.IsWebSocketRequest)
            {
                response.StatusCode = StatusCodes.Status400BadRequest; // Use StatusCodes for clarity
                return;
            }

            WebSocket webSocket = await response.HttpContext.WebSockets.AcceptWebSocketAsync();

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    IndicatorEvaluationResult[]? indicatorResults = _dataProcessor.GetLatestIndicatorResults();

                    if (indicatorResults != null && indicatorResults.Any()) // Ensure there are results to send
                    {
                        string json = System.Text.Json.JsonSerializer.Serialize(indicatorResults);
                        await webSocket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, cancellationToken);
                        _logger.LogTrace($"Sent {json.Length} bytes to client on the websocket channel");
                    }
                    // Consider making delay configurable or dynamic, or driven by data availability
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when the client disconnects or cancellation is triggered.
                _logger.LogInformation("StreamIndicators operation was canceled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StreamIndicators WebSocket.");
                // Ensure client is notified of an error if possible.
                if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "An unexpected error occurred.", CancellationToken.None);
                }
            }
            finally
            {
                // Ensure graceful closure if not already closed or aborted.
                // Use a new CancellationToken for CloseAsync if the original one was cancelled.
                if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Stream finished.", CancellationToken.None);
                }
                webSocket.Dispose(); // Dispose the WebSocket object
            }
        }

    }
}