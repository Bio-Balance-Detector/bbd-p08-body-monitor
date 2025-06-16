using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Services;
using BBD.BodyMonitor.Sessions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace BBD.BodyMonitor.Controllers
{
    /// <summary>
    /// Controller for system-level operations and information retrieval.
    /// This includes managing configuration, listing sessions, subjects, locations, and providing system status updates.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IDataProcessorService _dataProcessor;
        private readonly ISessionManagerService _sessionManager;
        private static DateTime lastIpCheck = DateTime.MinValue;
        private static List<string> ipAddresses = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="dataProcessor">The data processor service for handling data operations and configuration.</param>
        /// <param name="sessionManager">The session manager service for managing sessions, subjects, and locations.</param>
        public SystemController(ILogger<SystemController> logger, IDataProcessorService dataProcessor, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
            _sessionManager = sessionManager;
        }

        /// <summary>
        /// Retrieves the current application configuration.
        /// </summary>
        /// <returns>The current <see cref="BodyMonitorOptions"/>.</returns>
        /// <response code="200">Returns the current application configuration.</response>
        [HttpGet]
        [Route("getconfig")]
        public BodyMonitorOptions GetConfig()
        {
            return _dataProcessor.GetConfig();
        }

        /// <summary>
        /// Sets the application configuration.
        /// </summary>
        /// <param name="config">The <see cref="BodyMonitorOptions"/> to set.</param>
        /// <returns>The updated <see cref="BodyMonitorOptions"/> after applying the changes.</returns>
        /// <response code="200">Returns the updated application configuration.</response>
        /// <response code="400">If the provided configuration is invalid.</response>
        [HttpPost]
        [Route("setconfig")]
        public BodyMonitorOptions SetConfig(BodyMonitorOptions config)
        {
            _dataProcessor.SetConfig(config);
            return _dataProcessor.GetConfig();
        }

        /// <summary>
        /// Retrieves logs for a specific background task.
        /// </summary>
        /// <remarks>
        /// This method is currently not fully implemented and returns an empty array.
        /// </remarks>
        /// <param name="taskId">The ID of the task for which to retrieve logs.</param>
        /// <returns>An array of strings representing the task logs. Currently returns an empty array.</returns>
        /// <response code="200">Returns an array of task logs (currently always empty).</response>
        [HttpGet]
        [Route("gettasklogs")]
        public string[] GetTaskLogs(int taskId)
        {
            return new string[0];
        }

        /// <summary>
        /// Lists all available locations.
        /// </summary>
        /// <returns>An array of <see cref="Location"/> objects, or an empty array if none are found.</returns>
        /// <response code="200">Returns an array of available locations.</response>
        [HttpGet]
        [Route("listlocations")]
        public Location[] ListLocations()
        {
            return _sessionManager.ListLocations() ?? new Location[0];
        }

        /// <summary>
        /// Lists all available subjects.
        /// </summary>
        /// <returns>An array of <see cref="Subject"/> objects, or an empty array if none are found.</returns>
        /// <response code="200">Returns an array of available subjects.</response>
        [HttpGet]
        [Route("listsubjects")]
        public Subject[] ListSubjects()
        {
            return _sessionManager.ListSubjects() ?? new Subject[0];
        }

        /// <summary>
        /// Lists all available sessions.
        /// </summary>
        /// <returns>An array of <see cref="Session"/> objects, or an empty array if none are found.</returns>
        /// <response code="200">Returns an array of available sessions.</response>
        [HttpGet]
        [Route("listsessions")]
        public Session[] ListSessions()
        {
            return _sessionManager.ListSessions() ?? new Session[0];
        }

        /// <summary>
        /// Retrieves comprehensive system information.
        /// This includes IP addresses, connected devices, current configuration, locations, subjects, and sessions.
        /// </summary>
        /// <returns>A <see cref="SystemInformation"/> object containing various system details.</returns>
        /// <response code="200">Returns a comprehensive overview of the system's status and configuration.</response>
        /// <exception cref="Exception">Thrown if there is an issue retrieving system information components like IP addresses or device lists.</exception>
        [HttpGet]
        [Route("getsysteminformation")]
        public SystemInformation GetSystemInformation()
        {
            SystemInformation result = new();

            if (lastIpCheck < DateTime.Now.AddMinutes(-5))
            {
                lastIpCheck = DateTime.Now;

                try
                {
                    ipAddresses = new();
                    IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (IPAddress ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            ipAddresses.Add(ip.ToString());
                        }
                    }

                    // get our public IP via https://api.ipify.org
                    HttpClient client = new();
                    _ = client.GetStringAsync("https://api.ipify.org")
                        .ContinueWith(task =>
                        {
                            if (task.IsCompletedSuccessfully)
                            {
                                string publicIp = task.Result;
                                if (!string.IsNullOrEmpty(publicIp) && publicIp != "1.1.1.1")
                                {
                                    ipAddresses.Insert(0, publicIp);
                                }
                            }
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get IP addresses");
                }
            }
            result.IPAddresses = ipAddresses.ToArray();

            try
            {
                // get currently connected devices
                result.Devices = _dataProcessor.ListDevices();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get connected devices");
            }

            try
            {
                // get current configuration
                result.Configuration = GetConfig();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get configuration");
            }

            try
            {
                // get locations
                result.Locations = ListLocations();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get locations");
            }

            try
            {
                // get subjects
                result.Subjects = ListSubjects();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get subjects");
            }

            try
            {
                // get current sessions
                result.Sessions = ListSessions();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get sessions");
            }
            return result;
        }

        /// <summary>
        /// Establishes a WebSocket connection to stream system information in real-time.
        /// </summary>
        /// <remarks>
        /// This endpoint upgrades the HTTP GET request to a WebSocket connection.
        /// It continuously sends JSON serialized <see cref="SystemInformation"/> objects to the client.
        /// The stream closes when the client disconnects or the cancellation token is triggered.
        /// </remarks>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation of streaming system information.</returns>
        /// <response code="101">If the request is upgraded to a WebSocket connection.</response>
        /// <response code="400">If the request is not a WebSocket request.</response>
        /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled, e.g., client disconnects.</exception>
        /// <exception cref="Exception">Thrown if an unexpected error occurs during WebSocket communication.</exception>
        [HttpGet]
        [Route("streamsysteminformation")]
        public async Task StreamSystemInformation()
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
                    SystemInformation systemInformation = GetSystemInformation();

                    if (systemInformation != null)
                    {
                        string json = System.Text.Json.JsonSerializer.Serialize(systemInformation);
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