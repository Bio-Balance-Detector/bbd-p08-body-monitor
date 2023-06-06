using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Controllers;
using BBD.BodyMonitor.Models;
using BBD.BodyMonitor.Models.ThingSpeak;
using BBD.BodyMonitor.Services;
using BBD.BodyMonitor.Sessions;
using Fitbit.Api.Portable;
using Fitbit.Api.Portable.Models;
using Fitbit.Api.Portable.OAuth2;
using Fitbit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BBD.BodyMonitor.API.Controllers
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
            _thingSpeakOptions.Channel = Int32.Parse(_config["BodyMonitor:ThingSpeak:Channel"]);
        }

        [HttpGet]
        [Route("getsensordata/{channelId?}/{entryCount?}")]
        public SensorSegment[] GetSensorData(string? channelId, int entryCount = 10)
        {
            if (String.IsNullOrWhiteSpace(channelId) && (_thingSpeakOptions.Channel.HasValue))
            {
                channelId = _thingSpeakOptions.Channel.ToString();
            }

            HttpClient httpClient = new HttpClient();
            httpClient.GetAsync($"{_thingSpeakOptions.APIEndpoint}/channels/{channelId}/feeds.json?api_key={_thingSpeakOptions.APIKey}&results={entryCount}").ContinueWith((response) =>
            {
                var result = response.Result.Content.ReadAsStringAsync().Result;
                var feed = JsonSerializer.Deserialize<FeedsResponse>(result, new JsonSerializerOptions() { NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString });
                var segments = new List<SensorSegment>();
                foreach (var entry in feed.Feeds)
                {
                    var segment = new SensorSegment();
                    segment.Start = new DateTimeOffset(entry.CreatedAt, TimeSpan.Zero);
                    segment.End = new DateTimeOffset(entry.CreatedAt, TimeSpan.Zero).AddSeconds(15);
                    segment.SensorNames = new string?[4] { feed.Channel.Field1, feed.Channel.Field2, feed.Channel.Field3, feed.Channel.Field4 };
                    segment.SensorValues = new float?[4] { entry.Field1, entry.Field2, entry.Field3, entry.Field4 };
                    segments.Add(segment);
                }
                return segments.ToArray();
            });

            return new SensorSegment[0];
        }

        [HttpGet]
        [Route("savedata/{subjectAlias}/{date?}")]
        public void SaveFitbitData(string subjectAlias, DateTime? date)
        {
            //var subject = _sessionManager.GetSubject(subjectAlias);
            //if (subject == null)
            //{
            //    throw new Exception($"Subject '{subjectAlias}' not found.");
            //}

            //if (subject.FitbitEncodedID == null)
            //{
            //    throw new Exception($"The subject '{subject.Alias}' doesn't have a Fitbit ID defined.");
            //}

            //string encodedUserId = subject.FitbitEncodedID;

            //if (date == null)
            //{
            //    date = DateTime.UtcNow.AddDays(-30);
            //}

            //int daysToGet = (int)(DateTime.UtcNow.Subtract(date.Value).TotalDays + 1.5);

            //for (int i = 0; i <= daysToGet; i++)
            //{
            //    var currentDate = date.Value.AddDays(i);
            //    string fitbitSessionFilename = $"Subjects\\{subject.Alias}\\{subject.Alias}_{currentDate.ToString("yyyyMMdd_HHmmss")}__Fitbit.json";
            //    string fullpathToSessionFile = Path.Combine(_sessionManager.MetadataDirectory, fitbitSessionFilename);
            //    if (System.IO.File.Exists(fullpathToSessionFile))
            //    {
            //        if (currentDate.AddDays(3) < System.IO.File.GetLastWriteTimeUtc(fullpathToSessionFile))
            //        {
            //            // We have a supposedly complete file so we don't need to download the Fitbit data again.
            //            _logger.LogInformation($"Fitbit data on '{currentDate.ToString("yyyy-MM-dd")}' for subject '{subjectAlias}' seems to be up-to-date, so we don't download it.");
            //            continue;
            //        }
            //    }

            //    _logger.LogInformation($"Downloading Fitbit data on '{currentDate.ToString("yyyy-MM-dd")}' for subject '{subjectAlias}'.");

            //    var sleepData = GetSleepData(encodedUserId, currentDate);
            //    var heartRateData = GetHeartRate(encodedUserId, currentDate);

            //    var fitbitSession = new Session()
            //    {
            //        Version = 1,
            //        SegmentedData = new SegmentedData()
            //        {
            //            PotentialOfHydrogenSleep = sleepData,
            //            Temperature = heartRateData
            //        }
            //    };

            //    _sessionManager.SaveSession(fitbitSession, fitbitSessionFilename);
            //}

            //_sessionManager.RefreshDataDirectory();
        }
    }
}