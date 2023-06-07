using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.CustomJsonConverters;
using BBD.BodyMonitor.Sessions.Segments;
using System.Text.Json;

namespace BBD.BodyMonitor.Services
{
    public class SessionManagerService : ISessionManagerService
    {
        private Dictionary<Guid, Session> sessions = new Dictionary<Guid, Session>();

        private Dictionary<string, Session> aliases = new Dictionary<string, Session>();

        private Session? globalSettings;

        public string? DataDirectory { get; private set; }
        public string? MetadataDirectory { get; private set; }
        public string? SessionsDirectory { get; private set; }

        public Session[]? ListSessions()
        {
            return sessions.Values.ToArray();
        }

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

            return session;
        }

        public Session StartSession(string? locationAlias, string? subjectAlias)
        {
            var result = StartSession();

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

        public Session? FinishSession(Session? session = null)
        {
            lock (sessions)
            {
                if (session == null)
                {
                    session = GetSession(null);
                }

                if ((session != null) && (session.StartedAt.HasValue))
                {
                    session.FinishedAt = DateTimeOffset.Now;

                    if (session.SegmentedData != null)
                    {
                        session.SegmentedData.Sleep = TruncateSegmentedData(session.SegmentedData.Sleep, session.StartedAt.Value, session.FinishedAt.Value) as SleepSegment[];

                        session.SegmentedData.HeartRate = TruncateSegmentedData(session.SegmentedData.HeartRate, session.StartedAt.Value, session.FinishedAt.Value) as HeartRateSegment[];

                        session.SegmentedData.BloodTest = TruncateSegmentedData(session.SegmentedData.BloodTest, session.StartedAt.Value, session.FinishedAt.Value) as BloodTestSegment[];
                    }
                    sessions.Remove(session.Id);
                }
            }

            return session;
        }

        public Session ResetSession()
        {
            Session result;

            lock (sessions)
            {
                // get the currently running session
                var session = GetSession(null);

                // clone that session
                result = this.StartSession(session.Location?.Alias, session.Subject?.Alias);
                result.StartedAt = DateTimeOffset.Now;
                result.Name = session.Name;
                result.SegmentedData = new SegmentedData()
                {
                    BloodTest = session.SegmentedData?.BloodTest,
                    HeartRate = session.SegmentedData?.HeartRate,
                    Sleep = session.SegmentedData?.Sleep
                };
                result.Configuration = session.Configuration;

                session = this.FinishSession(session);
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

        public Session? GetSession(Guid? sessionId)
        {
            if (sessionId == null)
            {
                var sessions = this.ListSessions();
                if ((sessions != null) && (sessions.Length == 1))
                {
                    return sessions[0];
                }
            }
            else
            {
                if (sessions.TryGetValue(sessionId.Value, out var session))
                {
                    return session;
                }
            }

            return null;
        }

        public Session[]? GetRunningSessions()
        {
            return sessions.Values.Where(s => s.FinishedAt == null).ToArray();
        }

        public float? GetSessionValueProperty(Guid sessionId, string path)
        {
            var session = GetSession(sessionId);
            var propertyValue = GetProperty(session, path) as Single?;

            if (propertyValue == null)
            {
                throw new Exception($"Property {path} is not a float.");
            }

            return propertyValue;
        }

        public string GetSessionTextProperty(Guid sessionId, string path)
        {
            var session = GetSession(sessionId);
            var propertyValue = GetProperty(session, path) as String;

            if (propertyValue == null)
            {
                throw new Exception($"Property {path} is not a String.");
            }

            return propertyValue;
        }

        public Segment? GetSessionSegmentedProperty(Guid sessionId, string path, DateTimeOffset dateTimeOffset)
        {
            var session = GetSession(sessionId);
            var propertyValue = GetProperty(session, path) as Segment[];

            if (propertyValue == null)
            {
                throw new Exception($"Property {path} is not an array of Segment.");
            }

            return GetSegment(propertyValue, dateTimeOffset);
        }

        private Segment? GetSegment(Segment[] propertyValue, DateTimeOffset dateTimeOffset)
        {
            var segment = propertyValue.FirstOrDefault(s => s.Start <= dateTimeOffset && s.End >= dateTimeOffset);

            if (segment == null)
            {
                throw new Exception($"No segment found for {dateTimeOffset}.");
            }

            return segment;
        }

        private object? GetProperty(object? root, string path)
        {
            var pathSegments = path.Split('.');

            foreach (var propertyName in pathSegments)
            {
                if (root == null)
                {
                    return null;
                }

                var property = root.GetType().GetProperty(propertyName);
                if (property == null)
                {
                    throw new Exception($"Property '{propertyName}' not found in '{root.GetType().Name}'.");
                }

                root = property.GetValue(root);
            }

            return root;
        }
        public Session MergeSessionWith(Session session, string alias)
        {
            if (aliases.TryGetValue(alias, out var aliasSession))
            {
                MergeObjects<Session>(session, aliasSession);
            }

            return session;
        }

        public T MergeObjects<T>(T objectToMergeInto, T? objectToMerge)
        {
            if ((objectToMergeInto == null) || (objectToMerge == null))
            {
                return objectToMergeInto;
            }

            var sessionType = objectToMergeInto.GetType();
            foreach (var property in sessionType.GetProperties())
            {
                var originalValue = property.GetValue(objectToMergeInto);
                var newValue = property.GetValue(objectToMerge);
                if (originalValue == null)
                {
                    property.SetValue(objectToMergeInto, newValue);
                }
                else
                {
                    if ((property.PropertyType.IsClass) && (property.PropertyType.Name != "String") && (newValue != null))
                    {
                        if (property.PropertyType.IsArray)
                        {
                            // if we have to arrays, we need to append them
                            var originalArray = ((Array)originalValue);
                            var newArray = ((Array)newValue);
                            var mergedArray = Array.CreateInstance(property.PropertyType.GetElementType(), originalArray.Length + newArray.Length);
                            Array.Copy(originalArray, mergedArray, originalArray.Length);
                            Array.Copy(newArray, 0, mergedArray, originalArray.Length, newArray.Length);
                            property.SetValue(objectToMergeInto, mergedArray);
                        }
                        else
                        {
                            // we should look into the properties of the sub-class and merge it
                            var miMergeObjects = this.GetType().GetMethod("MergeObjects");
                            var genericMergeObjects = miMergeObjects?.MakeGenericMethod(property.PropertyType);
                            genericMergeObjects?.Invoke(this, new object[] { originalValue, newValue });
                        }
                    }
                }
            }

            return objectToMergeInto;
        }

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

                filename = $"BBD_{session.StartedAt.Value.ToString("yyyyMMdd_HHmmss")}_{session.Alias}.json";
                path = Path.Combine(SessionsDirectory, filename);
            }
            else
            {
                path = Path.Combine(MetadataDirectory, filename);
            }

            var sessionJson = JsonSerializer.Serialize(session, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(path, sessionJson);
        }

        public void SetDataDirectory(string dataDirectory)
        {
            if (!Directory.Exists(dataDirectory))
            {
                throw new FileNotFoundException($"Data directory {dataDirectory} does not exist");
            }

            this.DataDirectory = Path.GetFullPath(dataDirectory);
            this.MetadataDirectory = Path.Combine(this.DataDirectory, "Metadata");
            this.SessionsDirectory = Path.Combine(this.MetadataDirectory, "Sessions");

            string locationsDirectory = Path.Combine(this.MetadataDirectory, "Locations");
            string subjectsDirectory = Path.Combine(this.MetadataDirectory, "Subjects");

            if (!Directory.Exists(this.MetadataDirectory))
            {
                Directory.CreateDirectory(this.MetadataDirectory);
            }
            if (!Directory.Exists(this.SessionsDirectory))
            {
                Directory.CreateDirectory(this.SessionsDirectory);
            }
            if (!Directory.Exists(locationsDirectory))
            {
                Directory.CreateDirectory(locationsDirectory);
            }
            if (!Directory.Exists(subjectsDirectory))
            {
                Directory.CreateDirectory(subjectsDirectory);
            }


            globalSettings = LoadSessionFromFile(Path.Combine(this.MetadataDirectory, "GlobalSettings.json"));

            foreach (var file in Directory.GetFiles(locationsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var locationSession = LoadSessionFromFile(file);

                if ((locationSession != null) && (locationSession.Location != null))
                {
                    if (aliases.ContainsKey(locationSession.Location.Alias))
                    {
                        aliases.Remove(locationSession.Location.Alias);
                    }
                    aliases.Add(locationSession.Location.Alias, locationSession);
                }
            }

            foreach (var file in Directory.GetFiles(subjectsDirectory, "*.json", SearchOption.TopDirectoryOnly))
            {
                var subjectSession = LoadSessionFromFile(file);

                if ((subjectSession != null) && (subjectSession.Subject != null))
                {
                    if (aliases.ContainsKey(subjectSession.Subject.Alias))
                    {
                        aliases.Remove(subjectSession.Subject.Alias);
                    }
                    aliases.Add(subjectSession.Subject.Alias, subjectSession);

                    string subjectSubdirectory = Path.Combine(Path.GetDirectoryName(file), subjectSession.Subject.Alias);

                    if (Directory.Exists(subjectSubdirectory))
                    {
                        foreach (var partialSubjectFile in Directory.GetFiles(subjectSubdirectory, "*.json", SearchOption.AllDirectories))
                        {
                            var partialSubject = LoadSessionFromFile(partialSubjectFile);

                            if (partialSubject != null)
                            {
                                subjectSession = MergeObjects<Session>(subjectSession, partialSubject);
                            }
                        }
                    }
                }
            }
        }

        public void RefreshDataDirectory()
        {
            if (this.DataDirectory != null)
            {
                this.aliases.Clear();
                SetDataDirectory(DataDirectory);
            }
        }

        private Session? LoadSessionFromFile(string file)
        {
            Session? result = null;

            try
            {
                JsonSerializerOptions options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;
                options.Converters.Add(new CustomJsonConverterForNullableString());
                options.Converters.Add(new CustomJsonConverterForNullableDateTime());
                options.Converters.Add(new CustomJsonConverterForDateTimeOffset());
                options.Converters.Add(new CustomJsonConverterForNullableDateTimeOffset());

                var sessionJson = File.ReadAllText(file);

                result = JsonSerializer.Deserialize<Session>(sessionJson, options);
            }
            catch (Exception e)
            {
                // it's alright if we can't deserialize the file
            }

            return result;
        }

        public Subject? GetSubject(string alias)
        {
            if (aliases.TryGetValue(alias, out var session))
            {
                return session.Subject;
            }

            return null;
        }

        public Location? GetLocation(string alias)
        {
            if (aliases.TryGetValue(alias, out var session))
            {
                return session.Location;
            }

            return null;
        }
    }
}