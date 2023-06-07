using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Controllers;
using BBD.BodyMonitor.Services;
using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.Segments;
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
    public class FitbitController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IConfiguration _config;
        private readonly ISessionManagerService _sessionManager;
        private FitbitAppCredentials appCredentials = new FitbitAppCredentials();
        private string redirectURL;

        private static OAuth2AccessToken? currentAccessToken;
        private static string accessTokenFilename;
        private static UserProfile userProfile;

        public FitbitController(ILogger<SystemController> logger, IConfiguration configRoot, IOptionsMonitor<FitbitOptions> fitbitOptions, ISessionManagerService sessionManager)
        {
            _logger = logger;
            _config = configRoot;
            _sessionManager = sessionManager;

            _sessionManager.SetDataDirectory(_config["BodyMonitor:DataDirectory"]);

            appCredentials.ClientId = _config["BodyMonitor:Fitbit:ClientID"];
            appCredentials.ClientSecret = _config["BodyMonitor:Fitbit:ClientSecret"];
            redirectURL = _config["BodyMonitor:Fitbit:RedirectURL"];

            try
            {
                accessTokenFilename = Path.GetTempPath() + $"fitbit_{appCredentials.ClientId}.json";
                if (System.IO.File.Exists(accessTokenFilename))
                {
                    currentAccessToken = JsonSerializer.Deserialize<OAuth2AccessToken>(System.IO.File.ReadAllText(accessTokenFilename));
                }
            }
            catch
            {
                // don't worry if we can't read the token, we can always log in again
            }
        }

        [HttpGet]
        [Route("authenticate")]
        public string Authenticate()
        {
            var authenticator = new OAuth2Helper(appCredentials, redirectURL);
            string[] scopes = new string[] { "profile", "sleep", "heartrate", "respiratory_rate" };

            string authUrl = authenticator.GenerateAuthUrl(scopes, null);

            return authUrl;
        }

        [HttpGet]
        [Route("accesstoken")]
        public async void AccessToken(string code)
        {
            if (String.IsNullOrEmpty(appCredentials.ClientSecret))
            {
                throw new Exception("Fitbit client secret is not defined. Set the 'BODYMONITOR__FITBIT__CLIENTSECRET' enviroment variable to the client secret that you got when you registered your app at https://dev.fitbit.com/apps/.");
            }

            var authenticator = new OAuth2Helper(appCredentials, redirectURL);

            FitbitController.currentAccessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

            // save it for later use
            var accessTokenJson = JsonSerializer.Serialize(currentAccessToken, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(accessTokenFilename, accessTokenJson);
        }

        [HttpGet]
        [Route("getprofile/{encodedUserId}")]
        public UserProfile GetProfile(string encodedUserId)
        {
            if (FitbitController.currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            FitbitClient fitbitClient = new FitbitClient(appCredentials, FitbitController.currentAccessToken, true);
            var profile = fitbitClient.GetUserProfileAsync(encodedUserId).Result;

            FitbitController.userProfile = profile;
            return profile;
        }

        [HttpGet]
        [Route("getsleepdata/{encodedUserId}/{date}")]
        public SleepSegment[] GetSleepData(string encodedUserId, DateTime date)
        {
            if (FitbitController.currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            if (userProfile == null)
            {
                userProfile = GetProfile(encodedUserId);
            }

            FitbitClient fitbitClient = new FitbitClient(appCredentials, FitbitController.currentAccessToken, true);
            var sleepData = fitbitClient.GetSleepDateRangeAsync(date.Date.AddDays(-1), date.Date.AddDays(2), encodedUserId).Result;

            var sleepDataArray = sleepData.Sleep.SelectMany(s => s.Levels.Data).OrderBy(sld => sld.DateTime).ToArray();

            List<SleepSegment> result = new List<SleepSegment>();
            foreach (LevelsData ld in sleepDataArray)
            {
                // times are LOCAL times
                var ss = new SleepSegment()
                {
                    Start = new DateTimeOffset(ld.DateTime, TimeSpan.FromMilliseconds(userProfile.OffsetFromUTCMillis)),
                    End = new DateTimeOffset(ld.DateTime, TimeSpan.FromMilliseconds(userProfile.OffsetFromUTCMillis)).AddSeconds(ld.Seconds),
                };

                ss.Level = ld.Level switch
                {
                    "wake" => SleepLevel.Awake,
                    "awake" => SleepLevel.Awake,
                    "light" => SleepLevel.Light,
                    "deep" => SleepLevel.Deep,
                    "rem" => SleepLevel.REM,
                    "restless" => SleepLevel.Restless,
                    "asleep" => SleepLevel.Asleep,
                    _ => SleepLevel.Unknown,
                };

                if ((ss.Start.ToUniversalTime().Date != date.Date) && (ss.End.ToUniversalTime().Date != date.Date))
                {
                    continue;
                }

                result.Add(ss);
            }

            return result.ToArray();
        }

        [HttpGet]
        [Route("getheartrate/{encodedUserId}/{date}")]
        public HeartRateSegment[] GetHeartRate(string encodedUserId, DateTime date)
        {
            if (FitbitController.currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            if (userProfile == null)
            {
                userProfile = GetProfile(encodedUserId);
            }

            FitbitClient fitbitClient = new FitbitClient(appCredentials, FitbitController.currentAccessToken, true);
            var heartRateData = fitbitClient.GetHeartRateIntraday(date, HeartRateResolution.fiveSeconds, encodedUserId).Result;
            var heartRateDataList = heartRateData.Dataset.ToList();
            heartRateData = fitbitClient.GetHeartRateIntraday(date.AddDays(-1), HeartRateResolution.fiveSeconds, encodedUserId).Result;
            heartRateDataList.AddRange(heartRateData.Dataset);
            heartRateData = fitbitClient.GetHeartRateIntraday(date.AddDays(+1), HeartRateResolution.fiveSeconds, encodedUserId).Result;
            heartRateDataList.AddRange(heartRateData.Dataset);

            
            List<HeartRateSegment> result = new List<HeartRateSegment>();
            foreach (DatasetInterval di in heartRateDataList.OrderBy(hrd => hrd.TimeUtc).ToArray())
            {
                // times are UTC times
                var hrs = new HeartRateSegment()
                {
                    Start = new DateTimeOffset(di.TimeUtc, TimeSpan.Zero),
                    End = new DateTimeOffset(di.TimeUtc, TimeSpan.Zero).AddSeconds(5),
                    BeatsPerMinute = di.Value
                };

                if ((hrs.Start.ToUniversalTime().Date != date.Date) && (hrs.End.ToUniversalTime().Date != date.Date))
                {
                    continue;
                }

                result.Add(hrs);
            }
            return result.ToArray();
        }

        [HttpGet]
        [Route("savefitbitdata/{subjectAlias}/{date?}")]
        public void SaveFitbitData(string subjectAlias, DateTime? date)
        {
            if (FitbitController.currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            var subject = _sessionManager.GetSubject(subjectAlias);
            if (subject == null)
            {
                throw new Exception($"Subject '{subjectAlias}' not found.");
            }

            if (subject.FitbitEncodedID == null)
            {
                throw new Exception($"The subject '{subject.Alias}' doesn't have a Fitbit ID defined.");
            }

            string encodedUserId = subject.FitbitEncodedID;

            if (date == null)
            {
                date = DateTime.UtcNow.AddDays(-30);
            }

            int daysToGet = (int)(DateTime.UtcNow.Subtract(date.Value).TotalDays + 1.5);

            for (int i = 0; i <= daysToGet; i++)
            {
                var currentDate = date.Value.AddDays(i);
                string fitbitSessionFilename = $"Subjects\\{subject.Alias}\\{subject.Alias}_{currentDate.ToString("yyyyMMdd_HHmmss")}__Fitbit.json";
                string fullpathToSessionFile = Path.Combine(_sessionManager.MetadataDirectory, fitbitSessionFilename);
                if (System.IO.File.Exists(fullpathToSessionFile))
                {
                    if (currentDate.AddDays(3) < System.IO.File.GetLastWriteTimeUtc(fullpathToSessionFile))
                    {
                        // We have a supposedly complete file so we don't need to download the Fitbit data again.
                        _logger.LogInformation($"Fitbit data on '{currentDate.ToString("yyyy-MM-dd")}' for subject '{subjectAlias}' seems to be up-to-date, so we don't download it.");
                        continue;
                    }
                }

                _logger.LogInformation($"Downloading Fitbit data on '{currentDate.ToString("yyyy-MM-dd")}' for subject '{subjectAlias}'.");

                var sleepData = GetSleepData(encodedUserId, currentDate);
                var heartRateData = GetHeartRate(encodedUserId, currentDate);

                var fitbitSession = new Session()
                {
                    Version = 1,
                    SegmentedData = new SegmentedData()
                    {
                        Sleep = sleepData,
                        HeartRate = heartRateData
                    }
                };

                _sessionManager.SaveSession(fitbitSession, fitbitSessionFilename);
            }

            _sessionManager.RefreshDataDirectory();
        }
    }
}