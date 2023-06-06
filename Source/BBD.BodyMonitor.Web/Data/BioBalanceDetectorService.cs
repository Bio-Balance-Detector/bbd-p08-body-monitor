using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Environment;
using System.IO;
using System.Text.Json;

namespace BBD.BodyMonitor.Web.Data
{
    public class BioBalanceDetectorService
    {
        private readonly string _address;
        private readonly HttpClient _client = new HttpClient();

        public static SystemInformation? SystemInformation = null;

        public BioBalanceDetectorService(string address)
        {
            _address = address;
            _client.BaseAddress = new Uri(address);
        }

        public Task<SystemInformation?> GetSystemInformationAsync() => _client.GetFromJsonAsync<SystemInformation>("system/getsysteminformation");

        public void Start() => _client.GetAsync("dataacquisition/start");

        public void Stop() => _client.GetAsync("dataacquisition/stop");

        // call the StreamSystemInformation API endpoint
        public async void StreamSystemInformationAsync(CancellationToken cancellationToken)
        {
            var response = await _client.GetAsync("system/streamsysteminformation", HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var json = await reader.ReadLineAsync();
                var systemInformation = JsonSerializer.Deserialize<SystemInformation>(json);

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