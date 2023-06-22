using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.CustomJsonConverters;
using BBD.BodyMonitor.Sessions.Segments;
using System.Text.Json;

namespace BBD.BodyMonitor.Services
{
    public class SessionManagerService : ISessionManagerService
    {
        private readonly Dictionary<Guid, Session> sessions = new();

        private readonly Dictionary<string, Session> aliases = new();

        private Session? globalSettings;

        public string? DataDirectory { get; private set; }
        public string? MetadataDirectory { get; private set; }
        public string? SessionsDirectory { get; private set; }

        public Location[] ListLocations()
        {
            return aliases.Where(s => s.Value.Location != null).Select(s => s.Value.Location).ToArray() ?? Array.Empty<Location>();
        }

        public Subject[] ListSubjects()
        {
            return aliases.Where(s => s.Value.Subject != null).Select(s => s.Value.Subject).ToArray() ?? Array.Empty<Subject>();
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
                }
            }

            return session;
        }

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

        public Session[] ListSessions()
        {
            return sessions.Values.ToArray();
        }

        public Session[]? ListRunningSessions()
        {
            return sessions.Values.Where(s => s.FinishedAt == null).ToArray();
        }

        public float? GetSessionValueProperty(Guid sessionId, string path)
        {
            Session? session = GetSession(sessionId);
            float? propertyValue = GetProperty(session, path) as float?;

            return propertyValue == null ? throw new Exception($"Property {path} is not a float.") : propertyValue;
        }

        public string GetSessionTextProperty(Guid sessionId, string path)
        {
            Session? session = GetSession(sessionId);

            return GetProperty(session, path) is not string propertyValue ? throw new Exception($"Property {path} is not a String.") : propertyValue;
        }

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
        public Session MergeSessionWith(Session session, string alias)
        {
            if (aliases.TryGetValue(alias, out Session? aliasSession))
            {
                _ = MergeObjects<Session>(session, aliasSession);
            }

            return session;
        }

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
        }

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
        }

        public void RefreshDataDirectory()
        {
            if (DataDirectory != null)
            {
                aliases.Clear();
                SetDataDirectory(DataDirectory);
            }
        }

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
            catch (Exception)
            {
                // it's alright if we can't deserialize the file
            }

            return result;
        }

        public Subject? GetSubject(string alias)
        {
            return aliases.TryGetValue(alias, out Session? session) ? session.Subject : null;
        }

        public Location? GetLocation(string alias)
        {
            return aliases.TryGetValue(alias, out Session? session) ? session.Location : null;
        }
    }
}