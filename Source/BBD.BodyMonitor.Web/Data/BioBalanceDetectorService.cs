using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using System.Text.Json;

namespace BBD.BodyMonitor.Web.Data
{
    public class BioBalanceDetectorService
    {
        private static readonly HttpClient _client = new();

        public static SystemInformation? SystemInformation = null;

        public BioBalanceDetectorService()
        {
            _client.BaseAddress = new Uri("https://localhost:7061");
        }

        public void ChangeServerAddress(string? address)
        {
            if (!string.IsNullOrEmpty(address))
            {
                _client.BaseAddress = new Uri(address);
            }
        }

        public string? GetServerName()
        {
            return _client?.BaseAddress?.ToString();
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
    }
}