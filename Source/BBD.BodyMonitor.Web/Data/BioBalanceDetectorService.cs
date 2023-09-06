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

        public static SystemInformation? SystemInformation = null;

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

        // call the StreamSystemInformation API endpoint
        public async void StreamSystemInformationAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await _client.GetAsync("system/streamsysteminformation", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            _ = response.EnsureSuccessStatusCode();

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using StreamReader reader = new(stream);

            while (!reader.EndOfStream)
            {
                string? json = await reader.ReadLineAsync();
                SystemInformation? systemInformation = JsonSerializer.Deserialize<SystemInformation>(json);

                // Update a local variable with the new data as needed
                SystemInformation = systemInformation;
            }

            //Timer timer = new Timer((state) =>
            //{
            //    response.Content.ReadAsStream(cancellationToken) Async().Result.FlushAsync();
            //}, null, 0, 250);
        }

        // call the StreamIndicators API endpoint
        public async void StreamIndicatorsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Debug.Print("StreamIndicatorsAsync: connecting to the websockets endpoint");

                    ClientWebSocket clientWebSocket = new();
                    await clientWebSocket.ConnectAsync(new Uri("wss://localhost:7061/dataacquisition/streamindicators"), cancellationToken);

                    try
                    {
                        while (true)
                        {
                            ArraySegment<byte> buffer = new(new byte[1024 * 1024]);
                            WebSocketReceiveResult result = await clientWebSocket.ReceiveAsync(buffer, cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                                break;
                            }

                            byte[] data = buffer.Slice(0, result.Count).ToArray();
                            string json = System.Text.Encoding.UTF8.GetString(data);

                            IndicatorEvaluationResult[]? indicatorResults = JsonSerializer.Deserialize<IndicatorEvaluationResult[]?>(json);

                            // Update a local variable with the new data as needed
                            IndicatorResults = indicatorResults;

                            // Call the event handler
                            IndicatorsUpdated?.Invoke(this, indicatorResults);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"StreamIndicatorsAsync: exception {ex.Message}");
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