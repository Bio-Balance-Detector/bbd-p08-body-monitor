using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using BBD.BodyMonitor.Indicators;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text.Json;

namespace BBD.BodyMonitor.Web.Data
{
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

        public Task<SystemInformation?> GetSystemInformationAsync()
        {
            return _client.GetFromJsonAsync<SystemInformation>("system/getsysteminformation");
        }

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

        public void Start()
        {
            _ = _client.GetAsync("dataacquisition/start");
        }

        public void Stop()
        {
            _ = _client.GetAsync("dataacquisition/stop");
        }

        // connect to the StreamSystemInformation API endpoint
        public async void StreamSystemInformationAsync(CancellationToken cancellationToken)
        {
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
                    await systemInformationClientWebSocket.ConnectAsync(new Uri("wss://localhost:7061/system/streamsysteminformation"), cancellationToken);

                    ArraySegment<byte> buffer = new(new byte[64 * 1024 * 1024]);

                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            WebSocketReceiveResult result = await systemInformationClientWebSocket.ReceiveAsync(buffer, cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await systemInformationClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                                break;
                            }

                            byte[] data = buffer.Slice(0, result.Count).ToArray();
                            string json = System.Text.Encoding.UTF8.GetString(data);

                            SystemInformation? systemInformation = JsonSerializer.Deserialize<SystemInformation?>(json);

                            if (systemInformation != null)
                            {
                                // Update a local variable with the new data as needed
                                SystemInformation = systemInformation;

                                // Call the event handler
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
                    Debug.Print($"StreamSystemInformationAsync: exception {ex.Message}, waiting 1000 ms before reconnect attempt");
                    Thread.Sleep(1000);
                }

                Thread.Sleep(500);
            }
        }

        // connect to the StreamIndicators API endpoint
        public async void StreamIndicatorsAsync(CancellationToken cancellationToken)
        {
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
                    await indicatorsClientWebSocket.ConnectAsync(new Uri("wss://localhost:7061/dataacquisition/streamindicators"), cancellationToken);

                    ArraySegment<byte> buffer = new(new byte[64 * 1024 * 1024]);

                    try
                    {
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            WebSocketReceiveResult result = await indicatorsClientWebSocket.ReceiveAsync(buffer, cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await indicatorsClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                                break;
                            }

                            byte[] data = buffer.Slice(0, result.Count).ToArray();
                            string json = System.Text.Encoding.UTF8.GetString(data);

                            IndicatorEvaluationResult[]? indicatorResults = JsonSerializer.Deserialize<IndicatorEvaluationResult[]?>(json);

                            if ((indicatorResults != null) && (indicatorResults.Length > 0))
                            {
                                // Update a local variable with the new data as needed
                                IndicatorResults = indicatorResults;

                                // Call the event handler
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
                        indicatorsClientWebSocket = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"StreamIndicatorsAsync: exception {ex.Message}, waiting 1000 ms before reconnect attempt");
                    Thread.Sleep(1000);
                }

                Thread.Sleep(500);
            }
        }

        public IndicatorEvaluationResult? GetIndicator(string indicatorName)
        {
            return IndicatorResults?.FirstOrDefault(x => x.IndicatorName == indicatorName);
        }
    }
}