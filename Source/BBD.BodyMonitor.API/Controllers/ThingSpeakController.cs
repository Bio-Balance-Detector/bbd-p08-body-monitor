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
    /// <summary>
    /// Controller for interacting with the ThingSpeak IoT platform.
    /// It handles fetching sensor data from ThingSpeak channels and saving it locally.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class ThingSpeakController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IConfiguration _config;
        private readonly ISessionManagerService _sessionManager;

        private readonly ThingSpeakOptions _thingSpeakOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThingSpeakController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="configRoot">The root configuration interface.</param>
        /// <param name="thingSpeakOptions">The ThingSpeak options monitor.</param>
        /// <param name="sessionManager">The session manager service.</param>
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
        /// Retrieves sensor data entries from a specified ThingSpeak channel.
        /// </summary>
        /// <remarks>
        /// The maximum number of entries that can be fetched in a single request is 8000.
        /// Data is fetched in UTC timezone.
        /// </remarks>
        /// <param name="channelId">The ID of the ThingSpeak channel from which to retrieve data.</param>
        /// <param name="entryCount">Optional. The number of entries to retrieve. Defaults to 8000 (the maximum allowed by ThingSpeak).</param>
        /// <returns>An array of <see cref="SensorSegment"/> objects representing the fetched data. Returns an empty array if the request fails or no data is available.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="channelId"/> is null or whitespace.</exception>
        [HttpGet]
        [Route("getsensordata/{channelId}/{entryCount?}")]
        public SensorSegment[] GetSensorData(string channelId, int entryCount = 8000)
        {
            if (string.IsNullOrWhiteSpace(channelId))
            {
                throw new ArgumentNullException(nameof(channelId));
            }

            HttpClient httpClient = new();
            Task<SensorSegment[]> request = httpClient.GetAsync($"{_thingSpeakOptions.APIEndpoint}/channels/{channelId}/feeds.json?api_key={_thingSpeakOptions.APIKey}&results={entryCount}&timezone=Etc%2FUTC").ContinueWith((response) =>
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

        /// <summary>
        /// Fetches data from a subject's configured ThingSpeak channel and saves it into local session files.
        /// </summary>
        /// <remarks>
        /// This method retrieves the latest sensor data from the ThingSpeak channel associated with the given subject.
        /// It then organizes this data by date and merges it with any existing data in local session files.
        /// Session files are named using the pattern: "Subjects\{subjectAlias}\{subjectAlias}_{date:yyyyMMdd_HHmmss}__ThingSpeak.json".
        /// </remarks>
        /// <param name="subjectAlias">The alias of the subject for whom to save ThingSpeak data.</param>
        /// <exception cref="Exception">Thrown if the subject is not found or if the subject does not have a ThingSpeak channel defined.</exception>
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