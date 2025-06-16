using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.CustomJsonConverters;
using BBD.BodyMonitor.Sessions.Segments;
using System.Text.Json;

namespace BBD.BodyMonitor.Services
{
    /// <summary>
    /// Manages data acquisition sessions, including subject and location information,
    /// session lifecycle (start, finish, reset), and persistence of session data to the filesystem.
    /// It also provides methods to query session data properties.
    /// </summary>
    public class SessionManagerService : ISessionManagerService
    {
        private readonly ILogger<SessionManagerService> _logger;

        private readonly Dictionary<Guid, Session> sessions = new();

        private readonly Dictionary<string, Session> aliases = new();
        private Session? globalSettings;

        /// <summary>
        /// Gets the root directory where all body monitor data is stored.
        /// This path is configured when <see cref="SetDataDirectory"/> is called.
        /// </summary>
        public string? DataDirectory { get; private set; }
        /// <summary>
        /// Gets the directory where metadata files (like subject and location profiles, and global settings) are stored.
        /// This is typically a subdirectory named "Metadata" under <see cref="DataDirectory"/>.
        /// </summary>
        public string? MetadataDirectory { get; private set; }
        /// <summary>
        /// Gets the directory where session data files (in JSON format) are stored.
        /// This is typically a subdirectory named "Sessions" under the <see cref="MetadataDirectory"/>.
        /// </summary>
        public string? SessionsDirectory { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionManagerService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for recording service activity and errors.</param>
        public SessionManagerService(ILogger<SessionManagerService> logger)
        {
            _logger = (ILogger<SessionManagerService>?)logger;
        }

        /// <summary>
        /// Lists all available locations based on persisted location profiles loaded from the metadata directory.
        /// </summary>
        /// <returns>An array of <see cref="Location"/> objects. Returns an empty array if no locations are found.</returns>
        public Location[] ListLocations()
        {
            return aliases.Where(s => s.Value.Location != null).Select(s => s.Value.Location).ToArray() ?? Array.Empty<Location>();
        }

        /// <summary>
        /// Lists all available subjects based on persisted subject profiles loaded from the metadata directory.
        /// </summary>
        /// <returns>An array of <see cref="Subject"/> objects. Returns an empty array if no subjects are found.</returns>
        public Subject[] ListSubjects()
        {
            return aliases.Where(s => s.Value.Subject != null).Select(s => s.Value.Subject).ToArray() ?? Array.Empty<Subject>();
        }

        /// <summary>
        /// Starts a new, empty data acquisition session and applies global settings if available.
        /// </summary>
        /// <returns>The newly created <see cref="Session"/> object.</returns>
        public Session StartSession()
        {
            Session session;

            lock (sessions)
            {
                session = new Session
                {
                    Version = 1,
                    StartedAt = DateTimeOffset.Now
                };

                session = MergeObjects<Session>(session, globalSettings);

                sessions.Add(session.Id, session);
            }

            _logger.LogTrace($"Started new session with alias '{session.Alias}'.");

            return session;
        }

        /// <summary>
        /// Starts a new data acquisition session, optionally merging configuration from specified location and subject profiles.
        /// </summary>
        /// <param name="locationAlias">Optional alias of the location whose profile data should be merged into the session.</param>
        /// <param name="subjectAlias">Optional alias of the subject whose profile data should be merged into the session.</param>
        /// <returns>The newly created and configured <see cref="Session"/> object.</returns>
        public Session StartSession(string? locationAlias, string? subjectAlias)
        {
            Session result = StartSession();

            if (locationAlias != null)
            {
                result = MergeSessionWith(result, locationAlias);
            }

            if (subjectAlias != null)
            {
                result = MergeSessionWith(result, subjectAlias);
            }

            return result;
        }

        /// <summary>
        /// Finishes an ongoing data acquisition session by setting its end time and truncating segmented data to the session's duration.
        /// If no session is provided, it attempts to finish the most recent active session.
        /// </summary>
        /// <param name="session">Optional. The specific <see cref="Session"/> to finish. If null, the latest active session is targeted.</param>
        /// <returns>The finished <see cref="Session"/> object with its <see cref="Session.FinishedAt"/> timestamp set, or null if no session was active or found to finish.</returns>
        public Session? FinishSession(Session? session = null)
        {
            lock (sessions)
            {
                session ??= GetSession(null);

                if ((session != null) && session.StartedAt.HasValue)
                {
                    session.FinishedAt = DateTimeOffset.Now;

                    if (session.SegmentedData != null)
                    {
                        session.SegmentedData.Sleep = TruncateSegmentedData(session.SegmentedData.Sleep, session.StartedAt.Value, session.FinishedAt.Value) as SleepSegment[];

                        session.SegmentedData.HeartRate = TruncateSegmentedData(session.SegmentedData.HeartRate, session.StartedAt.Value, session.FinishedAt.Value) as HeartRateSegment[];

                        session.SegmentedData.BloodTest = TruncateSegmentedData(session.SegmentedData.BloodTest, session.StartedAt.Value, session.FinishedAt.Value) as BloodTestSegment[];
                    }
                    //_ = sessions.Remove(session.Id);
                    _logger.LogTrace($"Finished session '{session.Alias}'.");
                }
                else
                {
                    if (session == null)
                    {
                        _logger.LogTrace($"Can't finish session, because the session is null.");
                    }
                    else
                    {
                        _logger.LogTrace($"Can't finish session '{session.Alias}', because it hasn't started yet.");
                    }
                }
            }

            return session;
        }

        /// <summary>
        /// Resets the current session by finishing it and starting a new one with the same location, subject, and configuration, but with cleared segmented data.
        /// </summary>
        /// <returns>A new <see cref="Session"/> object representing the reset state, or null if no session was active to reset.</returns>
        public Session? ResetSession()
        {
            Session result;

            lock (sessions)
            {
                // get the currently running session
                Session? session = GetSession(null);

                if (session == null)
                {
                    return null;
                }

                // clone that session
                session = FinishSession(session);
                result = StartSession(session.Location?.Alias, session.Subject?.Alias);
                result.StartedAt = DateTimeOffset.Now;
                result.Name = session.Name;
                result.SegmentedData = new SegmentedData()
                {
                    BloodTest = session.SegmentedData?.BloodTest,
                    HeartRate = session.SegmentedData?.HeartRate,
                    Sleep = session.SegmentedData?.Sleep
                };
                result.Configuration = session.Configuration;

                //session = FinishSession(session);
                //this.SaveSession(session);

                //this.SaveSession(result);
            }

            return result;
        }

        private Segment[]? TruncateSegmentedData(Segment[]? segments, DateTimeOffset sessionStartedAt, DateTimeOffset sessionFinishedAt)
        {
            if (segments != null)
            {
                segments = segments.Where(sd => (sd.End > sessionStartedAt) && (sd.Start < sessionFinishedAt)).ToArray();
            }

            return segments;
        }

        /// <summary>
        /// Retrieves a session by its unique identifier. If no ID is provided, it attempts to return the single currently running session, if one exists.
        /// </summary>
        /// <param name="sessionId">Optional. The <see cref="Guid"/> of the session to retrieve. If null, attempts to find a single active session.</param>
        /// <returns>The <see cref="Session"/> object if found; otherwise, null.</returns>
        public Session? GetSession(Guid? sessionId)
        {
            if (sessionId == null)
            {
                Session[]? sessions = ListRunningSessions();
                if ((sessions != null) && (sessions.Length == 1))
                {
                    return sessions[0];
                }
            }
            else
            {
                if (sessions.TryGetValue(sessionId.Value, out Session? session))
                {
                    return session;
                }
            }

            return null;
        }

        /// <summary>
        /// Lists all sessions currently managed by the service, including both active and finished ones.
        /// </summary>
        /// <returns>An array of all <see cref="Session"/> objects.</returns>
        public Session[] ListSessions()
        {
            return sessions.Values.ToArray();
        }

        /// <summary>
        /// Lists all sessions that are currently active (i.e., have started but not finished).
        /// </summary>
        /// <returns>An array of active <see cref="Session"/> objects, or null/empty if no sessions are currently running.</returns>
        public Session[]? ListRunningSessions()
        {
            return sessions.Values.Where(s => s.FinishedAt == null).ToArray();
        }

        /// <summary>
        /// Retrieves a specific numeric (float) value from a session's data using a dot-separated path expression.
        /// </summary>
        /// <param name="sessionId">The ID of the session from which to retrieve the property.</param>
        /// <param name="path">The path to the value property (e.g., "SegmentedData.HeartRate.BeatsPerMinute", "Configuration.SomeNumericValue").</param>
        /// <returns>The float value if the property is found and is of a numeric type convertible to float; otherwise, null.</returns>
        /// <exception cref="Exception">Thrown if the property at the given path is not found or is not a float type.</exception>
        public float? GetSessionValueProperty(Guid sessionId, string path)
        {
            Session? session = GetSession(sessionId);
            float? propertyValue = GetProperty(session, path) as float?;

            return propertyValue == null ? throw new Exception($"Property {path} is not a float.") : propertyValue;
        }

        /// <summary>
        /// Retrieves a specific text (string) property from a session's data using a dot-separated path expression.
        /// </summary>
        /// <param name="sessionId">The ID of the session from which to retrieve the property.</param>
        /// <param name="path">The path to the text property (e.g., "Subject.Notes", "Location.Description").</param>
        /// <returns>The string value if the property is found and is of type string.</returns>
        /// <exception cref="Exception">Thrown if the property at the given path is not found or is not a string type.</exception>
        public string GetSessionTextProperty(Guid sessionId, string path)
        {
            Session? session = GetSession(sessionId);

            return GetProperty(session, path) is not string propertyValue ? throw new Exception($"Property {path} is not a String.") : propertyValue;
        }

        /// <summary>
        /// Retrieves a data segment from a session's segmented data collection (e.g., sleep stages, heart rate segments) that is active or relevant at the specified date and time.
        /// </summary>
        /// <param name="sessionId">The ID of the session.</param>
        /// <param name="path">The path to the segmented data collection within the session object (e.g., "SegmentedData.Sleep", "SegmentedData.HeartRate").</param>
        /// <param name="dateTimeOffset">The specific date and time for which to find the relevant segment.</param>
        /// <returns>The <see cref="Segment"/> if one is found that contains the specified <paramref name="dateTimeOffset"/> within its start and end times; otherwise, null.</returns>
        /// <exception cref="Exception">Thrown if the property at the given path is not an array of <see cref="Segment"/>, or if no segment is found for the specified time.</exception>
        public Segment? GetSessionSegmentedProperty(Guid sessionId, string path, DateTimeOffset dateTimeOffset)
        {
            Session? session = GetSession(sessionId);

            return GetProperty(session, path) is not Segment[] propertyValue
                ? throw new Exception($"Property {path} is not an array of Segment.")
                : GetSegment(propertyValue, dateTimeOffset);
        }

        private Segment? GetSegment(Segment[] propertyValue, DateTimeOffset dateTimeOffset)
        {
            Segment? segment = propertyValue.FirstOrDefault(s => s.Start <= dateTimeOffset && s.End >= dateTimeOffset);

            return segment ?? throw new Exception($"No segment found for {dateTimeOffset}.");
        }

        private object? GetProperty(object? root, string path)
        {
            string[] pathSegments = path.Split('.');

            foreach (string propertyName in pathSegments)
            {
                if (root == null)
                {
                    return null;
                }

                System.Reflection.PropertyInfo? property = root.GetType().GetProperty(propertyName);
                if (property == null)
                {
                    throw new Exception($"Property '{propertyName}' not found in '{root.GetType().Name}'.");
                }

                root = property.GetValue(root);
            }

            return root;
        }

        /// <summary>
        /// Merges data from a profile (identified by alias, e.g., a subject or location profile) into a target session.
        /// </summary>
        /// <param name="session">The target <see cref="Session"/> object to merge data into.</param>
        /// <param name="alias">The alias identifying the profile session whose data should be merged.</param>
        /// <returns>The modified <paramref name="session"/> object with data merged from the profile.</returns>
        public Session MergeSessionWith(Session session, string alias)
        {
            if (aliases.TryGetValue(alias, out Session? aliasSession))
            {
                _ = MergeObjects<Session>(session, aliasSession);
            }

            return session;
        }

        /// <summary>
        /// Recursively merges properties from a source object into a target object of the same type.
        /// If a property in the target is null, it takes the value from the source.
        /// For arrays, it concatenates them. For complex class properties, it recursively merges them.
        /// </summary>
        /// <typeparam name="T">The type of the objects to merge.</typeparam>
        /// <param name="objectToMergeInto">The target object that will receive merged values.</param>
        /// <param name="objectToMerge">The source object from which values are taken.</param>
        /// <returns>The modified <paramref name="objectToMergeInto"/>.</returns>
        public T MergeObjects<T>(T objectToMergeInto, T? objectToMerge)
        {
            if ((objectToMergeInto == null) || (objectToMerge == null))
            {
                return objectToMergeInto;
            }

            Type sessionType = objectToMergeInto.GetType();
            foreach (System.Reflection.PropertyInfo property in sessionType.GetProperties())
            {
                object? originalValue = property.GetValue(objectToMergeInto);
                object? newValue = property.GetValue(objectToMerge);
                if (originalValue == null)
                {
                    property.SetValue(objectToMergeInto, newValue);
                }
                else
                {
                    if (property.PropertyType.IsClass && (property.PropertyType.Name != "String") && (newValue != null))
                    {
                        if (property.PropertyType.IsArray)
                        {
                            // if we have to arrays, we need to append them
                            Array originalArray = (Array)originalValue;
                            Array newArray = (Array)newValue;
                            Array mergedArray = Array.CreateInstance(property.PropertyType.GetElementType(), originalArray.Length + newArray.Length);
                            Array.Copy(originalArray, mergedArray, originalArray.Length);
                            Array.Copy(newArray, 0, mergedArray, originalArray.Length, newArray.Length);
                            property.SetValue(objectToMergeInto, mergedArray);
                        }
                        else
                        {
                            // we should look into the properties of the sub-class and merge it
                            System.Reflection.MethodInfo? miMergeObjects = GetType().GetMethod("MergeObjects");
                            System.Reflection.MethodInfo? genericMergeObjects = miMergeObjects?.MakeGenericMethod(property.PropertyType);
                            _ = (genericMergeObjects?.Invoke(this, new object[] { originalValue, newValue }));
                        }
                    }
                }
            }

            return objectToMergeInto;
        }

        /// <summary>
        /// Saves the provided session object to a JSON file.
        /// If no filename is specified, a default filename is generated based on the session's start time and alias.
        /// If a filename is provided, it's relative to the metadata directory; otherwise, it's relative to the sessions directory.
        /// </summary>
        /// <param name="session">The <see cref="Session"/> object to save.</param>
        /// <param name="filename">Optional. The filename (relative path) to save the session to.
        /// If null, a default filename is used, and the session is saved in the <see cref="SessionsDirectory"/>.
        /// If provided, the path is relative to the <see cref="MetadataDirectory"/>.</param>
        /// <exception cref="Exception">Thrown if the session is null or if the session start time is not set when a filename is not provided.</exception>
        public void SaveSession(Session session, string? filename = null)
        {
            if (session == null)
            {
                throw new Exception("Session is not valid.");
            }

            string path;
            if (filename == null)
            {
                if (session.StartedAt == null)
                {
                    throw new Exception("The session starting time must be specified if you don't define an exact filename.");
                }

                filename = $"BBD_{session.StartedAt.Value:yyyyMMdd_HHmmss}_{session.Alias}.json";
                path = Path.Combine(SessionsDirectory, filename);
            }
            else
            {
                path = Path.Combine(MetadataDirectory, filename);
            }

            string sessionJson = JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // create the folder if it does not exist
            string folder = Path.GetDirectoryName(path);
            if (!Directory.Exists(folder))
            {
                _ = Directory.CreateDirectory(folder);
            }

            // write the file
            File.WriteAllText(path, sessionJson);

            _logger.LogTrace($"Session '{session.Alias}' saved to {path}.");
        }

        /// <summary>
        /// Sets the root data directory for storing all application data, including metadata and sessions.
        /// This method initializes subdirectories for metadata, sessions, locations, and subjects if they don't exist.
        /// It also loads global settings and all location/subject profiles from the specified directory structure.
        /// </summary>
        /// <param name="dataDirectory">The path to the root data directory.</param>
        /// <exception cref="FileNotFoundException">Thrown if the specified <paramref name="dataDirectory"/> does not exist.</exception>
        public void SetDataDirectory(string dataDirectory)
        {
            if (!Directory.Exists(dataDirectory))
            {
                throw new FileNotFoundException($"Data directory {dataDirectory} does not exist");
            }

            DataDirectory = Path.GetFullPath(dataDirectory);
            MetadataDirectory = Path.Combine(DataDirectory, "Metadata");
            SessionsDirectory = Path.Combine(MetadataDirectory, "Sessions");

            string locationsDirectory = Path.Combine(MetadataDirectory, "Locations");
            string subjectsDirectory = Path.Combine(MetadataDirectory, "Subjects");

            if (!Directory.Exists(MetadataDirectory))
            {
                _ = Directory.CreateDirectory(MetadataDirectory);
            }
            if (!Directory.Exists(SessionsDirectory))
            {
                _ = Directory.CreateDirectory(SessionsDirectory);
            }
            if (!Directory.Exists(locationsDirectory))
            {
                _ = Directory.CreateDirectory(locationsDirectory);
            }
            if (!Directory.Exists(subjectsDirectory))
            {
                _ = Directory.CreateDirectory(subjectsDirectory);
            }


            globalSettings = LoadSessionFromFile(Path.Combine(MetadataDirectory, "GlobalSettings.json"));

            foreach (string file in Directory.GetFiles(locationsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                Session? locationSession = LoadSessionFromFile(file);

                if ((locationSession != null) && (locationSession.Location != null))
                {
                    if (aliases.ContainsKey(locationSession.Location.Alias))
                    {
                        _ = aliases.Remove(locationSession.Location.Alias);
                    }
                    aliases.Add(locationSession.Location.Alias, locationSession);
                }
            }

            foreach (string file in Directory.GetFiles(subjectsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                Session? subjectSession = LoadSessionFromFile(file);

                if ((subjectSession != null) && (subjectSession.Subject != null))
                {
                    if (aliases.ContainsKey(subjectSession.Subject.Alias))
                    {
                        _ = aliases.Remove(subjectSession.Subject.Alias);
                    }
                    aliases.Add(subjectSession.Subject.Alias, subjectSession);

                    string subjectSubdirectory = Path.Combine(Path.GetDirectoryName(file), subjectSession.Subject.Alias);

                    if (Directory.Exists(subjectSubdirectory))
                    {
                        foreach (string partialSubjectFile in Directory.GetFiles(subjectSubdirectory, "*.json", SearchOption.AllDirectories))
                        {
                            Session? partialSubject = LoadSessionFromFile(partialSubjectFile);

                            if (partialSubject != null)
                            {
                                subjectSession = MergeObjects<Session>(subjectSession, partialSubject);
                            }
                        }
                    }
                }
            }
            _logger.LogTrace($"Session data folder was changed and {aliases.Count} locations and subjects were loaded to the manager.");
        }

        /// <summary>
        /// Refreshes the internal cache of location and subject profiles by rescanning the data directory.
        /// This is useful if profiles are added, removed, or modified externally.
        /// </summary>
        public void RefreshDataDirectory()
        {
            if (DataDirectory != null)
            {
                aliases.Clear();
                SetDataDirectory(DataDirectory);
            }
        }

        /// <summary>
        /// Loads a session object from a specified JSON file.
        /// </summary>
        /// <param name="file">The full path to the session file.</param>
        /// <returns>The loaded <see cref="Session"/> object, or null if deserialization fails or the file is not found.</returns>
        public Session? LoadSessionFromFile(string file)
        {
            Session? result = null;
            try
            {
                JsonSerializerOptions options = new()
                {
                    PropertyNameCaseInsensitive = true
                };
                options.Converters.Add(new CustomJsonConverterForNullableString());
                options.Converters.Add(new CustomJsonConverterForNullableDateTime());
                options.Converters.Add(new CustomJsonConverterForDateTimeOffset());
                options.Converters.Add(new CustomJsonConverterForNullableDateTimeOffset());

                string sessionJson = File.ReadAllText(file);

                result = JsonSerializer.Deserialize<Session>(sessionJson, options);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                _logger?.LogDebug($"Session file '{file}' not found.");
            }
            catch (Exception ex)
            {
                // it's alright if we can't deserialize the file
                _logger?.LogWarning(ex, $"Could not deserialize file '{file}'.");
            }

            return result;
        }

        /// <summary>
        /// Retrieves a subject's profile based on their alias.
        /// </summary>
        /// <param name="alias">The alias of the subject to retrieve.</param>
        /// <returns>The <see cref="Subject"/> object if a profile with the given alias is found; otherwise, null.</returns>
        public Subject? GetSubject(string alias)
        {
            return aliases.TryGetValue(alias, out Session? session) ? session.Subject : null;
        }

        /// <summary>
        /// Retrieves a location's profile based on its alias.
        /// </summary>
        /// <param name="alias">The alias of the location to retrieve.</param>
        /// <returns>The <see cref="Location"/> object if a profile with the given alias is found; otherwise, null.</returns>
        public Location? GetLocation(string alias)
        {
            return aliases.TryGetValue(alias, out Session? session) ? session.Location : null;
        }
    }
}