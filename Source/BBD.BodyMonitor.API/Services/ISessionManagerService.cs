using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.Segments;

namespace BBD.BodyMonitor.Services
{
    /// <summary>
    /// Defines the contract for services that manage data acquisition sessions,
    /// including subject and location information, session lifecycle, and data persistence.
    /// </summary>
    public interface ISessionManagerService
    {
        /// <summary>
        /// Gets the root directory where all body monitor data is stored.
        /// </summary>
        string? DataDirectory { get; }

        /// <summary>
        /// Gets the directory where metadata files (like subject and location profiles) are stored.
        /// This is typically a subdirectory under <see cref="DataDirectory"/>.
        /// </summary>
        string? MetadataDirectory { get; }

        /// <summary>
        /// Gets the directory where session data files are stored.
        /// This is typically a subdirectory under <see cref="DataDirectory"/>.
        /// </summary>
        string? SessionsDirectory { get; }

        /// <summary>
        /// Lists all available locations based on persisted location profiles.
        /// </summary>
        /// <returns>An array of <see cref="Location"/> objects.</returns>
        Location[] ListLocations();

        /// <summary>
        /// Lists all available subjects based on persisted subject profiles.
        /// </summary>
        /// <returns>An array of <see cref="Subject"/> objects.</returns>
        Subject[] ListSubjects();

        /// <summary>
        /// Lists all persisted sessions.
        /// </summary>
        /// <returns>An array of <see cref="Session"/> objects.</returns>
        Session[] ListSessions();

        /// <summary>
        /// Starts a new data acquisition session with the specified location and subject.
        /// If location or subject aliases are not provided or not found, defaults may be used or new profiles created.
        /// </summary>
        /// <param name="locationAlias">Optional alias of the location for the session.</param>
        /// <param name="subjectAlias">Optional alias of the subject for the session.</param>
        /// <returns>The newly created <see cref="Session"/> object.</returns>
        Session StartSession(string? locationAlias, string? subjectAlias);

        /// <summary>
        /// Finishes an ongoing data acquisition session. If no session is provided, it attempts to finish the most recent active session.
        /// </summary>
        /// <param name="session">Optional. The specific <see cref="Session"/> to finish. If null, the latest active session is targeted.</param>
        /// <returns>The finished <see cref="Session"/> object, or null if no session was active or found to finish.</returns>
        Session? FinishSession(Session? session = null);

        /// <summary>
        /// Resets the current session state, typically clearing any ongoing session information.
        /// </summary>
        /// <returns>A new, empty <see cref="Session"/> object representing the reset state.</returns>
        Session ResetSession();

        /// <summary>
        /// Retrieves a specific session by its unique identifier.
        /// </summary>
        /// <param name="sessionId">The <see cref="Guid"/> of the session to retrieve.</param>
        /// <returns>The <see cref="Session"/> object if found, otherwise null.</returns>
        Session GetSession(Guid? sessionId);

        /// <summary>
        /// Lists all sessions that are currently considered active or running.
        /// </summary>
        /// <returns>An array of running <see cref="Session"/> objects, or null/empty if none are running.</returns>
        Session[]? ListRunningSessions();

        /// <summary>
        /// Retrieves a specific numeric value property from a session's data using a path-like expression.
        /// </summary>
        /// <param name="sessionId">The ID of the session.</param>
        /// <param name="path">The path to the value property (e.g., "SegmentedData.HeartRate.BeatsPerMinute").</param>
        /// <returns>The float value if found, otherwise null.</returns>
        float? GetSessionValueProperty(Guid sessionId, string path);

        /// <summary>
        /// Retrieves a specific text property from a session's data using a path-like expression.
        /// </summary>
        /// <param name="sessionId">The ID of the session.</param>
        /// <param name="path">The path to the text property.</param>
        /// <returns>The string value if found, otherwise null or an empty string.</returns>
        string GetSessionTextProperty(Guid sessionId, string path);

        /// <summary>
        /// Retrieves a specific segment from a session's segmented data that is active or relevant at the given date and time.
        /// </summary>
        /// <param name="sessionId">The ID of the session.</param>
        /// <param name="path">The path to the segmented data collection (e.g., "SegmentedData.Sleep").</param>
        /// <param name="dateTimeOffset">The date and time to find the relevant segment for.</param>
        /// <returns>The <see cref="Segment"/> if found, otherwise null.</returns>
        Segment? GetSessionSegmentedProperty(Guid sessionId, string path, DateTimeOffset dateTimeOffset);

        /// <summary>
        /// Merges data from another subject's session into the specified session.
        /// </summary>
        /// <param name="session">The primary session to merge data into.</param>
        /// <param name="alias">The alias of the subject whose data should be merged from.</param>
        /// <returns>The updated <see cref="Session"/> object with merged data.</returns>
        Session MergeSessionWith(Session session, string alias);

        /// <summary>
        /// Loads a session from a specified JSON file.
        /// </summary>
        /// <param name="file">The relative path to the session file within the sessions directory.</param>
        /// <returns>The loaded <see cref="Session"/> object, or null if loading fails or the file doesn't exist.</returns>
        public Session? LoadSessionFromFile(string file);

        /// <summary>
        /// Saves the provided session object to a file. If no filename is provided, a default filename is generated.
        /// </summary>
        /// <param name="session">The <see cref="Session"/> object to save.</param>
        /// <param name="filename">Optional. The filename to save the session to. If null, a default is used based on session properties.</param>
        void SaveSession(Session session, string? filename = null);

        /// <summary>
        /// Sets the root data directory for all session and metadata storage.
        /// </summary>
        /// <param name="dataDirectory">The path to the data directory.</param>
        void SetDataDirectory(string dataDirectory);

        /// <summary>
        /// Retrieves a subject's profile by their alias.
        /// </summary>
        /// <param name="alias">The alias of the subject.</param>
        /// <returns>The <see cref="Subject"/> object if found, otherwise null.</returns>
        Subject? GetSubject(string alias);

        /// <summary>
        /// Retrieves a location's profile by its alias.
        /// </summary>
        /// <param name="alias">The alias of the location.</param>
        /// <returns>The <see cref="Location"/> object if found, otherwise null.</returns>
        Location? GetLocation(string alias);

        /// <summary>
        /// Refreshes the internal cache or listing of data files, typically by rescanning the data directory.
        /// </summary>
        void RefreshDataDirectory();
    }
}
