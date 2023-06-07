using BBD.BodyMonitor.Sessions;
using BBD.BodyMonitor.Sessions.Segments;

namespace BBD.BodyMonitor.Services
{
    public interface ISessionManagerService
    {
        string? DataDirectory { get; }
        string? MetadataDirectory { get; }
        string? SessionsDirectory { get; }

        Session[]? ListSessions();
        Session StartSession(string? locationAlias, string? subjectAlias);
        Session? FinishSession(Session? session = null);
        Session ResetSession();
        Session GetSession(Guid? sessionId);
        Session[]? GetRunningSessions();
        float? GetSessionValueProperty(Guid sessionId, string path);
        string GetSessionTextProperty(Guid sessionId, string path);
        Segment? GetSessionSegmentedProperty(Guid sessionId, string path, DateTimeOffset dateTimeOffset);
        Session MergeSessionWith(Session session, string alias);
        void SaveSession(Session session, string? filename = null);
        void SetDataDirectory(string dataDirectory);
        Subject? GetSubject(string alias);
        Location? GetLocation(string alias);
        void RefreshDataDirectory();
    }
}
