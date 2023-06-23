using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Services;
using BBD.BodyMonitor.Sessions;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Sockets;

namespace BBD.BodyMonitor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SystemController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IDataProcessorService _dataProcessor;
        private readonly ISessionManagerService _sessionManager;
        private static DateTime lastIpCheck = DateTime.MinValue;
        private static List<string> ipAddresses = new();

        public SystemController(ILogger<SystemController> logger, IDataProcessorService dataProcessor, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _dataProcessor = dataProcessor;
            _sessionManager = sessionManager;
        }

        [HttpGet]
        [Route("getconfig")]
        public BodyMonitorOptions GetConfig()
        {
            return _dataProcessor.GetConfig();
        }

        [HttpPost]
        [Route("setconfig")]
        public BodyMonitorOptions SetConfig(BodyMonitorOptions config)
        {
            _dataProcessor.SetConfig(config);
            return _dataProcessor.GetConfig();
        }

        [HttpGet]
        [Route("gettasklogs")]
        public string[] GetTaskLogs(int taskId)
        {
            return new string[0];
        }

        [HttpGet]
        [Route("listlocations")]
        public Location[] ListLocations()
        {
            return _sessionManager.ListLocations() ?? new Location[0];
        }

        [HttpGet]
        [Route("listsubjects")]
        public Subject[] ListSubjects()
        {
            return _sessionManager.ListSubjects() ?? new Subject[0];
        }

        [HttpGet]
        [Route("listsessions")]
        public Session[] ListSessions()
        {
            return _sessionManager.ListSessions() ?? new Session[0];
        }

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

        // Add a streaming endpoint for the system information
        [HttpGet]
        [Route("streamsysteminformation")]
        public async Task StreamSystemInformation()
        {
            HttpResponse response = Response;
            response.Headers.Add("Content-Type", "text/event-stream");
            response.Headers.Add("Cache-Control", "no-cache");
            response.Headers.Add("Connection", "keep-alive");

            StreamWriter writer = new(response.Body, System.Text.Encoding.UTF8);

            while (!response.HttpContext.RequestAborted.IsCancellationRequested)
            {
                SystemInformation systemInformation = GetSystemInformation();
                await writer.WriteAsync(System.Text.Json.JsonSerializer.Serialize(systemInformation));
                await writer.WriteAsync("\n");
                await writer.FlushAsync();
                await Task.Delay(500);
            }
        }
    }
}