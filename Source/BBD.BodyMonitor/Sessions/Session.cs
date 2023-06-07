using BBD.BodyMonitor.Configuration;
using BBD.BodyMonitor.Sessions.Segments;

namespace BBD.BodyMonitor.Sessions
{
    public class Session : DeidentifiedData
    {
        public int Version { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
        public Location? Location { get; set; }
        public Subject? Subject { get; set; }
        public SegmentedData? SegmentedData { get; set; }
        public BodyMonitorOptions? Configuration { get; set; }


        public bool TryToGetValue(string path, DateTimeOffset startTime, DateTimeOffset endTime, out float value)
        {
            value = 0;

            if (path == "Session.SegmentedData.Sleep.Level")
            {
                SleepSegment[]? segments = SegmentedData?.Sleep?.Where(s => s.End >= startTime && s.Start <= endTime).ToArray();

                if ((segments == null) || (segments.Length == 0))
                {
                    return false;
                }

                if (segments.Length == 1)
                {
                    value = (float)segments[0].Level;
                }

                if (segments.Length > 1)
                {
                    value = segments.Select(s => (float)s.Level).Average();
                }

                return true;
            }

            if (path == "Session.SegmentedData.HeartRate.BeatsPerMinute")
            {
                HeartRateSegment[]? segments = SegmentedData?.HeartRate?.Where(s => s.End >= startTime && s.Start <= endTime).ToArray();

                if ((segments == null) || (segments.Length == 0))
                {
                    return false;
                }

                if (segments.Length == 1)
                {
                    value = segments[0].BeatsPerMinute;
                }

                if (segments.Length > 1)
                {
                    value = segments.Select(s => s.BeatsPerMinute).Average();
                }

                return true;
            }

            if (path == "Session.SegmentedData.BloodTest.Cholesterol")
            {
                BloodTestSegment? segment = SegmentedData?.BloodTest?.Where(s => s.End >= startTime && s.Start <= endTime).FirstOrDefault();

                if (segment == null)
                {
                    return false;
                }

                value = (float)segment.Cholesterol;
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            string sa = StartedAt == null ? "" : $"S:{StartedAt}";
            string l = Location == null ? "" : $"L:{Location?.Alias} '{Location?.Name}'";
            string s = Subject == null ? "" : $"S:{Subject?.Alias} '{Subject?.Name}'";
            string c = Configuration == null ? "" : "C:present";

            return $"V:{Version} {sa} {l} {s} {c}";
        }
    }
}
