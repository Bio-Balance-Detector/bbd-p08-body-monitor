using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Models.ThingSpeak;
using BBD.BodyMonitor.Services;
using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.Segments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BBD.BodyMonitor.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ThingSpeakController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IConfiguration _config;
        private readonly ISessionManagerService _sessionManager;

        private readonly ThingSpeakOptions _thingSpeakOptions;


        public ThingSpeakController(ILogger<SystemController> logger, IConfiguration configRoot, IOptionsMonitor<ThingSpeakOptions> thingSpeakOptions, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _config = configRoot;
            _sessionManager = sessionManager;

            _sessionManager.SetDataDirectory(_config["BodyMonitor:DataDirectory"]);

            _thingSpeakOptions = thingSpeakOptions.CurrentValue;

            _thingSpeakOptions.APIEndpoint = new Uri(_config["BodyMonitor:ThingSpeak:APIEndpoint"]);
            _thingSpeakOptions.APIKey = _config["BodyMonitor:ThingSpeak:APIKey"];
        }

        /// <summary>
        /// Get entries from a ThingSpeak channel. The maximum number of entries is 8000.
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="entryCount"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("getsensordata/{channelId}/{entryCount?}")]
        public SensorSegment[] GetSensorData(string channelId, int entryCount = 8000)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            HttpClient httpClient = new();
            Task<SensorSegment[]> request = httpClient.GetAsync($"{_thingSpeakOptions.APIEndpoint}/channels/{channelId}/feeds.json?api_key={_thingSpeakOptions.APIKey}&results={entryCount}").ContinueWith((response) =>
            {
                string result = response.Result.Content.ReadAsStringAsync().Result;
                FeedsResponse? feed = JsonSerializer.Deserialize<FeedsResponse>(result, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });
                List<SensorSegment> segments = new();
                foreach (Feed entry in feed.Feeds)
                {
                    SensorSegment segment = new()
                    {
                        Start = new DateTimeOffset(entry.CreatedAt, TimeSpan.Zero),
                        End = new DateTimeOffset(entry.CreatedAt, TimeSpan.Zero).AddSeconds(15),
                        SensorNames = new string?[4] { feed.Channel.Field1, feed.Channel.Field2, feed.Channel.Field3, feed.Channel.Field4 },
                        SensorValues = new float?[4] { entry.Field1, entry.Field2, entry.Field3, entry.Field4 }
                    };
                    segments.Add(segment);
                }
                return segments.ToArray();
            });

            request.Wait();

            return request.IsCompletedSuccessfully ? request.Result : (new SensorSegment[0]);
        }

        [HttpGet]
        [Route("savedata/{subjectAlias}")]
        public void SaveThingSpeakData(string subjectAlias)
        {
            Sessions.Subject? subject = _sessionManager.GetSubject(subjectAlias);
            if (subject == null)
            {
                throw new Exception($"Subject '{subjectAlias}' not found.");
            }

            if (subject.ThingSpeakChannel == null)
            {
                throw new Exception($"The subject '{subject.Alias}' doesn't have a ThingSpeak channel defined.");
            }

            SensorSegment[] latestEntries = GetSensorData(subject.ThingSpeakChannel);
            IEnumerable<IGrouping<DateTime, SensorSegment>> entryStartDateGroups = latestEntries.OrderBy(e => e.Start).GroupBy(e => e.Start.Date);

            foreach (IGrouping<DateTime, SensorSegment> ssg in entryStartDateGroups)
            {
                DateTime currentDate = ssg.Key;
                SensorSegment[] entriesInGroup = ssg.ToArray();

                Session? thingSpeakSession = null;

                // try to load the session file
                string thingSpeakSessionFilename = $"Subjects\\{subject.Alias}\\{subject.Alias}_{currentDate:yyyyMMdd_HHmmss}__ThingSpeak.json";
                string fullpathToSessionFile = Path.Combine(_sessionManager.MetadataDirectory, thingSpeakSessionFilename);
                if (System.IO.File.Exists(fullpathToSessionFile))
                {
                    // Read the session file
                    thingSpeakSession = _sessionManager.LoadSessionFromFile(thingSpeakSessionFilename);
                }

                // Create a new session if we didn't/couldn't load one
                thingSpeakSession ??= new()
                {
                    Version = 1,
                    SegmentedData = new SegmentedData()
                    {
                        Sensors = new SensorSegment[0]
                    }
                };

                _logger.LogInformation($"Saving ThingSpeak sensor data on '{currentDate:yyyy-MM-dd}' for subject '{subjectAlias}'.");

                // Merge the new entries with the existing ones
                List<SensorSegment> entriesToSave = new(thingSpeakSession.SegmentedData.Sensors);
                entriesToSave.AddRange(entriesInGroup);

                // Remove dupicates from the merged entries, sort them by start time
                thingSpeakSession.SegmentedData.Sensors = entriesToSave.GroupBy(e => e.Start).Select(g => g.First()).OrderBy(e => e.Start).ToArray();

                // Save the session file
                _sessionManager.SaveSession(thingSpeakSession, thingSpeakSessionFilename);
            }

            //_sessionManager.RefreshDataDirectory();
        }
    }
}