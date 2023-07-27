namespace BBD.BodyMonitor.Configuration
{
    /// <summary>
    /// Definition of a signal schedule. It sets the channel, the time to start, the time to stop, the repeat period, the signal name and the signal length.
    /// (eg. "W1,*/4m,Stimulation-A2(30.9s)", "W2,09:15/5m,Stimulation-A3(94s)", "W2,11:30,Stimulation-A1(22s)", "W2,15:00->15:25/30s,Stimulation-A3(12.77s)")
    /// </summary>
    public class ScheduleOptions
    {
        /// <summary>
        /// Channel ID. It must be one of the channel IDs of the signal generating device (eg. "W1" or "W2").
        /// </summary>
        public required string ChannelId { get; set; }
        /// <summary>
        /// Time to start the signal. It can be a specific time (eg. 19:15), or a relative time (eg. * means the time when the application started).
        /// </summary>
        public TimeSpan? TimeToStart { get; set; }
        /// <summary>
        /// Time to stop the signal. Optional, it can be a specific time (eg. 19:17).
        /// </summary>
        public TimeSpan? TimeToStop { get; set; }
        /// <summary>
        /// Repeat period of the signal. Optional, it can be a specific time period (eg. /30s, /4m, /1h).
        /// </summary>
        public TimeSpan? RepeatPeriod { get; set; }
        /// <summary>
        /// Signal name. It must be one of the signal definitions.
        /// </summary>
        public required string SignalName { get; set; }
        /// <summary>
        /// Signal length. Tt can be a specific time length (eg. (30s), (4m), (1h)). The signal definition will be mapped to the specified time length.
        /// </summary>
        public required TimeSpan SignalLength { get; set; }

        public static ScheduleOptions Parse(string input)
        {
            string[] parts = input.Split(',');

            string[] timeParts = parts[1].Split('/');

            string timeToStart = timeParts[0].Contains("->") ? timeParts[0].Split("->")[0] : timeParts[0];
            string? timeToEnd = timeParts[0].Contains("->") ? timeParts[0].Split("->")[1] : null;
            string? repeatPeriod = timeParts.Length > 1 ? timeParts[1] : null;

            string signalName = parts[2][..parts[2].IndexOf('(')];
            string signalLength = parts[2].Substring(parts[2].IndexOf('(') + 1, parts[2].IndexOf(')') - parts[2].IndexOf('(') - 1);

            return new ScheduleOptions
            {
                ChannelId = parts[0],
                TimeToStart = TimeSpanParse(timeToStart).Value,
                TimeToStop = TimeSpanParse(timeToEnd),
                RepeatPeriod = TimeSpanParse(repeatPeriod),
                SignalName = signalName,
                SignalLength = TimeSpanParse(signalLength).Value
            };
        }

        public override string ToString()
        {
            string? timePart = TimeToStart.ToString();

            if (TimeToStop.HasValue)
            {
                timePart += "->" + TimeToStop;
            }

            if (RepeatPeriod.HasValue)
            {
                timePart += "/" + RepeatPeriod;
            }

            string signalPart = SignalName + "(" + SignalLength + ")";

            return $"{ChannelId},{timePart},{signalPart}";
        }

        private static TimeSpan? TimeSpanParse(string? str)
        {
            TimeSpan? result = null;
            str = str?.Trim().ToLower();
            string[]? colonParts = str?.Split(':');

            if (string.IsNullOrWhiteSpace(str))
            {
                result = null;
            }
            else if (str == "*")
            {
                result = new TimeSpan(DateTime.UtcNow.Hour, DateTime.UtcNow.Minute, DateTime.UtcNow.Second);
            }
            else if (colonParts.Length == 2)
            {
                result = new TimeSpan(int.Parse(colonParts[0]), int.Parse(colonParts[1]), 0);
            }
            else if (colonParts.Length == 3)
            {
                result = new TimeSpan(int.Parse(colonParts[0]), int.Parse(colonParts[1]), int.Parse(colonParts[2]));
            }
            else if ((colonParts.Length == 1) && (colonParts[0].Length >= 2))
            {
                char unit = colonParts[0].Last();
                double number = double.Parse(colonParts[0][..^1], System.Globalization.CultureInfo.InvariantCulture);

                switch (unit)
                {
                    case 'h':
                        result = TimeSpan.FromHours(number);
                        break;
                    case 'm':
                        result = TimeSpan.FromMinutes(number);
                        break;
                    case 's':
                        result = TimeSpan.FromSeconds(number);
                        break;
                }
            }

            if ((result == null) && TimeSpan.TryParse(str, out TimeSpan parsedResult))
            {
                result = parsedResult;
            }

            return result;
        }
    }
}