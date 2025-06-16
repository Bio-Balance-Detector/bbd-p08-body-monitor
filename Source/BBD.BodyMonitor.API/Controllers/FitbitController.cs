using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Services;
using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.Segments;
using Fitbit.Api.Portable;
using Fitbit.Api.Portable.Models;
using Fitbit.Api.Portable.OAuth2;
using Fitbit.Models;
using Fitbit.Portable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BBD.BodyMonitor.Controllers
{
    /// <summary>
    /// Controller for interacting with the Fitbit API.
    /// It handles authentication, fetching user profile, sleep data, and heart rate data.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class FitbitController : ControllerBase
    {
        private readonly ILogger<SystemController> _logger;
        private readonly IConfiguration _config;
        private readonly ISessionManagerService _sessionManager;
        private readonly FitbitAppCredentials appCredentials = new();
        private readonly string redirectURL;

        private static OAuth2AccessToken? currentAccessToken;
        private static string accessTokenFilename;
        private static UserProfile userProfile;

        /// <summary>
        /// Initializes a new instance of the <see cref="FitbitController"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging messages.</param>
        /// <param name="configRoot">The root configuration interface.</param>
        /// <param name="fitbitOptions">The Fitbit options monitor.</param>
        /// <param name="sessionManager">The session manager service.</param>
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

        /// <summary>
        /// Generates the Fitbit authentication URL.
        /// </summary>
        /// <remarks>
        /// This endpoint initiates the OAuth 2.0 authentication process with Fitbit.
        /// The user should be redirected to the returned URL to authorize the application.
        /// </remarks>
        /// <returns>The Fitbit authentication URL.</returns>
        /// <response code="200">Returns the Fitbit authentication URL.</response>
        [HttpGet]
        [Route("authenticate")]
        public string Authenticate()
        {
            OAuth2Helper authenticator = new(appCredentials, redirectURL);
            string[] scopes = new string[] { "profile", "sleep", "heartrate", "respiratory_rate" };

            string authUrl = authenticator.GenerateAuthUrl(scopes, null);

            return authUrl;
        }

        /// <summary>
        /// Exchanges the authentication code for an access token.
        /// </summary>
        /// <remarks>
        /// This endpoint is called by Fitbit after the user authorizes the application.
        /// The provided code is used to obtain an access token, which is then stored for future API calls.
        /// </remarks>
        /// <param name="code">The authentication code provided by Fitbit.</param>
        /// <returns>An awaitable task representing the asynchronous operation.</returns>
        /// <response code="200">Indicates the access token was successfully obtained and stored.</response>
        /// <response code="400">If the provided code is invalid or the exchange fails.</response>
        /// <exception cref="Exception">Thrown if the Fitbit client secret is not defined.</exception>
        [HttpGet]
        [Route("accesstoken")]
        public async void AccessToken(string code)
        {
            if (string.IsNullOrEmpty(appCredentials.ClientSecret))
            {
                throw new Exception("Fitbit client secret is not defined. Set the 'BODYMONITOR__FITBIT__CLIENTSECRET' enviroment variable to the client secret that you got when you registered your app at https://dev.fitbit.com/apps/.");
            }

            OAuth2Helper authenticator = new(appCredentials, redirectURL);

            currentAccessToken = await authenticator.ExchangeAuthCodeForAccessTokenAsync(code);

            // save it for later use
            string accessTokenJson = JsonSerializer.Serialize(currentAccessToken, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(accessTokenFilename, accessTokenJson);
        }

        /// <summary>
        /// Retrieves the Fitbit user profile for the specified user.
        /// </summary>
        /// <param name="encodedUserId">The encoded ID of the Fitbit user. Use "-" for the currently authenticated user.</param>
        /// <returns>The <see cref="UserProfile"/> object containing the user's profile information.</returns>
        /// <response code="200">Returns the user's Fitbit profile.</response>
        /// <response code="401">If the user is not authenticated (Fitbit access token is not available).</response>
        /// <exception cref="Exception">Thrown if the Fitbit access token is not available (i.e., user is not authenticated).</exception>
        [HttpGet]
        [Route("getprofile/{encodedUserId}")]
        public UserProfile GetProfile(string encodedUserId)
        {
            if (currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            FitbitClient fitbitClient = new(appCredentials, currentAccessToken, true);
            UserProfile profile = fitbitClient.GetUserProfileAsync(encodedUserId).Result;

            userProfile = profile;
            return profile;
        }

        /// <summary>
        /// Retrieves sleep data for the specified user and date.
        /// </summary>
        /// <param name="encodedUserId">The encoded ID of the Fitbit user. Use "-" for the currently authenticated user.</param>
        /// <param name="date">The date for which to retrieve sleep data.</param>
        /// <returns>An array of <see cref="SleepSegment"/> objects representing the sleep periods for the given date.</returns>
        /// <response code="200">Returns the sleep data for the specified user and date.</response>
        /// <response code="401">If the user is not authenticated (Fitbit access token is not available).</response>
        /// <exception cref="Exception">Thrown if the Fitbit access token is not available (i.e., user is not authenticated).</exception>
        [HttpGet]
        [Route("getsleepdata/{encodedUserId}/{date}")]
        public SleepSegment[] GetSleepData(string encodedUserId, DateTime date)
        {
            if (currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            userProfile ??= GetProfile(encodedUserId);

            FitbitClient fitbitClient = new(appCredentials, currentAccessToken, true);
            SleepDateRangeBase sleepData = fitbitClient.GetSleepDateRangeAsync(date.Date.AddDays(-1), date.Date.AddDays(2), encodedUserId).Result;

            LevelsData[] sleepDataArray = sleepData.Sleep.SelectMany(s => s.Levels.Data).OrderBy(sld => sld.DateTime).ToArray();

            List<SleepSegment> result = new();
            foreach (LevelsData ld in sleepDataArray)
            {
                // times are LOCAL times
                SleepSegment ss = new()
                {
                    Start = new DateTimeOffset(ld.DateTime, TimeSpan.FromMilliseconds(userProfile.OffsetFromUTCMillis)),
                    End = new DateTimeOffset(ld.DateTime, TimeSpan.FromMilliseconds(userProfile.OffsetFromUTCMillis)).AddSeconds(ld.Seconds),
                    Level = ld.Level switch
                    {
                        "wake" => SleepLevel.Awake,
                        "awake" => SleepLevel.Awake,
                        "light" => SleepLevel.Light,
                        "deep" => SleepLevel.Deep,
                        "rem" => SleepLevel.REM,
                        "restless" => SleepLevel.Restless,
                        "asleep" => SleepLevel.Asleep,
                        _ => SleepLevel.Unknown,
                    }
                };

                if (ss.Start.ToUniversalTime().Date != date.Date && ss.End.ToUniversalTime().Date != date.Date)
                {
                    continue;
                }

                result.Add(ss);
            }

            return result.ToArray();
        }

        /// <summary>
        /// Retrieves heart rate data for the specified user and date.
        /// </summary>
        /// <param name="encodedUserId">The encoded ID of the Fitbit user. Use "-" for the currently authenticated user.</param>
        /// <param name="date">The date for which to retrieve heart rate data.</param>
        /// <returns>An array of <see cref="HeartRateSegment"/> objects representing the heart rate data for the given date.</returns>
        /// <response code="200">Returns the heart rate data for the specified user and date.</response>
        /// <response code="401">If the user is not authenticated (Fitbit access token is not available).</response>
        /// <exception cref="Exception">Thrown if the Fitbit access token is not available (i.e., user is not authenticated).</exception>
        [HttpGet]
        [Route("getheartrate/{encodedUserId}/{date}")]
        [Obsolete("This method for fetching heart rate is obsolete. Consider using newer Fitbit API endpoints or a different approach if available.")]
        public HeartRateSegment[] GetHeartRate(string encodedUserId, DateTime date)
        {
            if (currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            userProfile ??= GetProfile(encodedUserId);

            FitbitClient fitbitClient = new(appCredentials, currentAccessToken, true);
            HeartActivitiesIntraday heartRateData = fitbitClient.GetHeartRateIntraday(date, HeartRateResolution.fiveSeconds, encodedUserId).Result;
            List<DatasetInterval> heartRateDataList = heartRateData.Dataset.ToList();
            heartRateData = fitbitClient.GetHeartRateIntraday(date.AddDays(-1), HeartRateResolution.fiveSeconds, encodedUserId).Result;
            heartRateDataList.AddRange(heartRateData.Dataset);
            heartRateData = fitbitClient.GetHeartRateIntraday(date.AddDays(+1), HeartRateResolution.fiveSeconds, encodedUserId).Result;
            heartRateDataList.AddRange(heartRateData.Dataset);


            List<HeartRateSegment> result = new();
            foreach (DatasetInterval di in heartRateDataList.OrderBy(hrd => hrd.TimeUtc).ToArray())
            {
                // times are UTC times
                HeartRateSegment hrs = new()
                {
                    Start = new DateTimeOffset(di.TimeUtc, TimeSpan.Zero),
                    End = new DateTimeOffset(di.TimeUtc, TimeSpan.Zero).AddSeconds(5),
                    BeatsPerMinute = di.Value
                };

                if (hrs.Start.ToUniversalTime().Date != date.Date && hrs.End.ToUniversalTime().Date != date.Date)
                {
                    continue;
                }

                result.Add(hrs);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Downloads and saves Fitbit data (sleep and heart rate) for a given subject and date range.
        /// </summary>
        /// <remarks>
        /// This method is obsolete and may be removed in future versions.
        /// It iterates from the specified date (or 30 days ago if null) up to the current day,
        /// downloading and saving data for each day if it doesn't already exist or if existing data is outdated.
        /// </remarks>
        /// <param name="subjectAlias">The alias of the subject for whom to save data.</param>
        /// <param name="date">Optional. The starting date from which to download data. If null, defaults to 30 days prior to the current UTC date.</param>
        /// <response code="200">Indicates that the data saving process was initiated (or completed if no new data was needed).</response>
        /// <response code="400">If the subject is not found or the subject does not have a Fitbit ID defined.</response>
        /// <response code="401">If the user is not authenticated (Fitbit access token is not available).</response>
        /// <exception cref="Exception">Thrown if the Fitbit access token is not available, subject is not found, or subject's Fitbit ID is not defined.</exception>
        [HttpGet]
        [Route("savefitbitdata/{subjectAlias}/{date?}")]
        [Obsolete("This method for saving Fitbit data is obsolete and may be removed in future versions. It is recommended to implement a more robust data synchronization strategy.")]
        public void SaveFitbitData(string subjectAlias, DateTime? date)
        {
            if (currentAccessToken == null)
            {
                throw new Exception("Fitbit access token is null. You must first call 'fitbit/authenticate' and follow the URL that is returned.");
            }

            Subject? subject = _sessionManager.GetSubject(subjectAlias);
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
                DateTime currentDate = date.Value.AddDays(i);
                string fitbitSessionFilename = $"Subjects\\{subject.Alias}\\{subject.Alias}_{currentDate:yyyyMMdd_HHmmss}__Fitbit.json";
                string fullpathToSessionFile = Path.Combine(_sessionManager.MetadataDirectory, fitbitSessionFilename);
                if (System.IO.File.Exists(fullpathToSessionFile))
                {
                    if (currentDate.AddDays(3) < System.IO.File.GetLastWriteTimeUtc(fullpathToSessionFile))
                    {
                        // We have a supposedly complete file so we don't need to download the Fitbit data again.
                        _logger.LogInformation($"Fitbit data on '{currentDate:yyyy-MM-dd}' for subject '{subjectAlias}' seems to be up-to-date, so we don't download it.");
                        continue;
                    }
                }

                _logger.LogInformation($"Downloading Fitbit data on '{currentDate:yyyy-MM-dd}' for subject '{subjectAlias}'.");

                SleepSegment[] sleepData = GetSleepData(encodedUserId, currentDate);
                HeartRateSegment[] heartRateData = GetHeartRate(encodedUserId, currentDate);

                Session fitbitSession = new()
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