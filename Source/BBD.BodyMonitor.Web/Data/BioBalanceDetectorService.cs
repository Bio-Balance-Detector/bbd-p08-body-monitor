using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Indicators;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;

namespace BBD.BodyMonitor.Web.Data
{
    /// <summary>
    /// Service class for interacting with the BioBalanceDetector API.
    /// It handles HTTP requests and WebSocket connections for system information and indicators.
    /// </summary>
    public class BioBalanceDetectorService
    {
        private static ServerOptions _server = new();
        private static readonly HttpClient _client = new();

        private readonly Mutex _streamMutex = new();

        private static ClientWebSocket? systemInformationClientWebSocket = null;

        public static SystemInformation? SystemInformation = null;

        private static ClientWebSocket? indicatorsClientWebSocket = null;

        public static IndicatorEvaluationResult[]? IndicatorResults = null;

        public event EventHandler<SystemInformation>? SystemInformationUpdated;

        public event EventHandler<IndicatorEvaluationResult[]?>? IndicatorsUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="BioBalanceDetectorService"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        public BioBalanceDetectorService(IConfiguration configuration)
        {
            ServerOptions? server = configuration?.GetSection("Servers")?.Get<ServerOptions[]>()?.FirstOrDefault();

            server ??= new()
            {
                Name = "Localhost",
                Address = "https://localhost:7061"
            };

            ChangeServer(server);
        }

        /// <summary>
        /// Changes the server address for the API.
        /// </summary>
        /// <param name="server">The server options with the new address.</param>
        public void ChangeServer(ServerOptions? server)
        {
            if (server != null)
            {
                _server = server;
                _client.BaseAddress = new Uri(server.Address);
            }
        }

        public string? GetServerName()
        {
            return _server?.Name;
        }

        /// <summary>
        /// Gets the system information from the API.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the system information.</returns>
        public Task<SystemInformation?> GetSystemInformationAsync()
        {
            return _client.GetFromJsonAsync<SystemInformation>("system/getsysteminformation");
        }

        /// <summary>
        /// Gets the configuration from the API.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the body monitor options.</returns>
        public async Task<BodyMonitorOptions?> GetConfigAsync()
        {
            // Send a GET request to the API endpoint
            HttpResponseMessage responseMessage = _client.GetAsync("system/getconfig").Result;

            // Ensure the response is successful before proceeding
            _ = responseMessage.EnsureSuccessStatusCode();

            // Parse the response body as JSON
            string json = await responseMessage.Content.ReadAsStringAsync();

            JsonSerializerOptions options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
            };

            // Deserialize the JSON String to BodyMonitorOptions object
            BodyMonitorOptions? bodyMonitorOptions = JsonSerializer.Deserialize<BodyMonitorOptions>(json, options);

            return bodyMonitorOptions;

            //return _client.GetFromJsonAsync<BodyMonitorOptions>("system/getconfig");
        }

        /// <summary>
        /// Sets the configuration for the API.
        /// </summary>
        /// <param name="config">The body monitor options to set.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the updated body monitor options.</returns>
        public async Task<BodyMonitorOptions?> SetConfigAsync(BodyMonitorOptions config)
        {
            // Send a POST request to the API endpoint
            HttpResponseMessage responseMessage = _client.PostAsJsonAsync("system/setconfig", config).Result;

            // Ensure the response is successful before proceeding
            _ = responseMessage.EnsureSuccessStatusCode();

            // Parse the response body as JSON
            string json = await responseMessage.Content.ReadAsStringAsync();

            // Deserialize the JSON String to BodyMonitorOptions object
            BodyMonitorOptions bodyMonitorOptions = JsonSerializer.Deserialize<BodyMonitorOptions>(json);

            return bodyMonitorOptions;
        }

        /// <summary>
        /// Starts the data acquisition process.
        /// </summary>
        public void Start()
        {
            _ = _client.GetAsync("dataacquisition/start");
        }

        /// <summary>
        /// Stops the data acquisition process.
        /// </summary>
        public void Stop()
        {
            _ = _client.GetAsync("dataacquisition/stop");
        }

        /// <summary>
        /// Connects to the StreamSystemInformation API endpoint and streams system information.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async void StreamSystemInformationAsync(CancellationToken cancellationToken)
        {
            // If a WebSocket connection is already established, do nothing.
            if (systemInformationClientWebSocket != null)
            {
                return;
            }


            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Debug.Print("StreamSystemInformationAsync: connecting to the websockets endpoint");

                    systemInformationClientWebSocket = new();
                    // Connect to the WebSocket endpoint.
                    await systemInformationClientWebSocket.ConnectAsync(new Uri("wss://localhost:7061/system/streamsysteminformation"), cancellationToken);

                    // Buffer to store received data.
                    ArraySegment<byte> buffer = new(new byte[64 * 1024 * 1024]); // 64 MB buffer

                    try
                    {
                        // Loop to continuously receive messages.
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            WebSocketReceiveResult result = await systemInformationClientWebSocket.ReceiveAsync(buffer, cancellationToken);

                            // If the server closes the connection, break the loop.
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await systemInformationClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                                break;
                            }

                            // Process the received data.
                            byte[] data = buffer.Slice(0, result.Count).ToArray();
                            string json = System.Text.Encoding.UTF8.GetString(data);

                            SystemInformation? systemInformation = JsonSerializer.Deserialize<SystemInformation?>(json);

                            if (systemInformation != null)
                            {
                                // Update a local variable with the new data as needed
                                SystemInformation = systemInformation;

                                // Call the event handler to notify subscribers.
                                SystemInformationUpdated?.Invoke(this, systemInformation);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"StreamSystemInformationAsync: exception {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Log connection errors and retry after a delay.
                    Debug.Print($"StreamSystemInformationAsync: exception {ex.Message}, waiting 1000 ms before reconnect attempt");
                    Thread.Sleep(1000); // Wait for 1 second before retrying.
                }

                Thread.Sleep(500); // Wait for 0.5 seconds before the next attempt in the outer loop.
            }
        }

        /// <summary>
        /// Connects to the StreamIndicators API endpoint and streams indicator data.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        public async void StreamIndicatorsAsync(CancellationToken cancellationToken)
        {
            // If a WebSocket connection is already established, do nothing.
            if (indicatorsClientWebSocket != null)
            {
                return;
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Debug.Print("StreamIndicatorsAsync: connecting to the websockets endpoint");

                    indicatorsClientWebSocket = new();
                    // Connect to the WebSocket endpoint.
                    await indicatorsClientWebSocket.ConnectAsync(new Uri("wss://localhost:7061/dataacquisition/streamindicators"), cancellationToken);

                    // Buffer to store received data.
                    ArraySegment<byte> buffer = new(new byte[64 * 1024 * 1024]); // 64 MB buffer

                    try
                    {
                        // Loop to continuously receive messages.
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            WebSocketReceiveResult result = await indicatorsClientWebSocket.ReceiveAsync(buffer, cancellationToken);

                            // If the server closes the connection, break the loop.
                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await indicatorsClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                                break;
                            }

                            // Process the received data.
                            byte[] data = buffer.Slice(0, result.Count).ToArray();
                            string json = System.Text.Encoding.UTF8.GetString(data);

                            IndicatorEvaluationResult[]? indicatorResults = JsonSerializer.Deserialize<IndicatorEvaluationResult[]?>(json);

                            if ((indicatorResults != null) && (indicatorResults.Length > 0))
                            {
                                // Update a local variable with the new data as needed
                                IndicatorResults = indicatorResults;

                                // Call the event handler to notify subscribers.
                                IndicatorsUpdated?.Invoke(this, indicatorResults);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"StreamIndicatorsAsync: exception {ex.Message}");
                    }
                    finally
                    {
                        // Ensure the WebSocket client is reset for the next connection attempt.
                        indicatorsClientWebSocket = null;
                    }
                }
                catch (Exception ex)
                {
                    // Log connection errors and retry after a delay.
                    Debug.Print($"StreamIndicatorsAsync: exception {ex.Message}, waiting 1000 ms before reconnect attempt");
                    Thread.Sleep(1000); // Wait for 1 second before retrying.
                }

                Thread.Sleep(500); // Wait for 0.5 seconds before the next attempt in the outer loop.
            }
        }

        /// <summary>
        /// Gets a specific indicator by its name.
        /// </summary>
        /// <param name="indicatorName">The name of the indicator to retrieve.</param>
        /// <returns>The <see cref="IndicatorEvaluationResult"/> if found; otherwise, null.</returns>
        public IndicatorEvaluationResult? GetIndicator(string indicatorName)
        {
            return IndicatorResults?.FirstOrDefault(x => x.IndicatorName == indicatorName);
        }
    }
}