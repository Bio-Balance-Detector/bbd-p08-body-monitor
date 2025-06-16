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
        /// Local time to start the signal. It can be a specific time (eg. 19:15), or a relative time (eg. * means the time when the application started).
        /// </summary>
        public TimeSpan TimeToStart { get; set; }
        /// <summary>
        /// Local time to stop the signal. Optional, it can be a specific time (eg. 19:17).
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

        /// <summary>
        /// Parses a string representation of a schedule into a <see cref="ScheduleOptions"/> object.
        /// </summary>
        /// <param name="input">The string to parse. Expected format: "ChannelId,StartTime[->StopTime][/RepeatPeriod],SignalName(SignalLength)".
        /// Examples: "W1,*/4m,Stimulation-A2(30.9s)", "W2,09:15/5m,Stimulation-A3(94s)", "W2,11:30,Stimulation-A1(22s)", "W2,15:00->15:25/30s,Stimulation-A3(12.77s)"</param>
        /// <returns>A new <see cref="ScheduleOptions"/> object.</returns>
        /// <exception cref="FormatException">Thrown if the input string is not in the expected format.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the input string is null or empty.</exception>
        public static ScheduleOptions Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentNullException(nameof(input), "Input string cannot be null or empty.");
            }

            string[] parts = input.Split(',');

            if (parts.Length != 3)
            {
                throw new FormatException($"Input string '{input}' is not in the expected format 'ChannelId,TimeSpec,SignalSpec'.");
            }

            string[] timeParts = parts[1].Split('/');

            string timeToStart = timeParts[0].Contains("->") ? timeParts[0].Split("->")[0] : timeParts[0];
            string? timeToEnd = timeParts[0].Contains("->") ? timeParts[0].Split("->")[1] : null;
            string? repeatPeriod = timeParts.Length > 1 ? timeParts[1] : null;

            string signalNameString = parts[2];
            int signalNameStartIndex = signalNameString.IndexOf('(');
            int signalNameEndIndex = signalNameString.LastIndexOf(')');

            if (signalNameStartIndex == -1 || signalNameEndIndex == -1 || signalNameEndIndex < signalNameStartIndex)
            {
                throw new FormatException($"Signal part '{signalNameString}' is not in the expected format 'SignalName(SignalLength)'.");
            }

            string signalName = signalNameString[..signalNameStartIndex];
            string signalLength = signalNameString.Substring(signalNameStartIndex + 1, signalNameEndIndex - signalNameStartIndex - 1);

            TimeSpan? parsedTimeToStart = TimeSpanParse(timeToStart);
            if (!parsedTimeToStart.HasValue)
            {
                throw new FormatException($"Could not parse TimeToStart from '{timeToStart}'.");
            }
            TimeSpan timeSpanToStart = parsedTimeToStart.Value;

            TimeSpan? timeSpanToStop = timeToEnd == null ? null : TimeSpanParse(timeToEnd);

            if ((timeSpanToStop != null) && (timeSpanToStop < timeSpanToStart))
            {
                // Assuming stop time on the next day if it's earlier than start time
                timeSpanToStop = timeSpanToStop.Value.Add(TimeSpan.FromDays(1));
            }

            TimeSpan? parsedSignalLength = TimeSpanParse(signalLength);
            if (!parsedSignalLength.HasValue)
            {
                throw new FormatException($"Could not parse SignalLength from '{signalLength}'.");
            }

            return new ScheduleOptions
            {
                ChannelId = parts[0].Trim(),
                TimeToStart = timeSpanToStart,
                TimeToStop = timeSpanToStop,
                RepeatPeriod = TimeSpanParse(repeatPeriod),
                SignalName = signalName.Trim(),
                SignalLength = parsedSignalLength.Value
            };
        }

        /// <summary>
        /// Returns a string representation of the schedule in the format "ChannelId,StartTime[->StopTime][/RepeatPeriod],SignalName(SignalLength)".
        /// </summary>
        /// <returns>A string representation of the schedule.</returns>
        public override string ToString()
        {
            string timePart = TimeToStart.ToString(@"hh\:mm\:ss");

            if (TimeToStop.HasValue)
            {
                timePart += "->" + TimeToStop.Value.ToString(@"hh\:mm\:ss");
            }

            if (RepeatPeriod.HasValue)
            {
                timePart += "/" + FormatTimeSpanForSchedule(RepeatPeriod.Value);
            }

            string signalPart = $"{SignalName}({FormatTimeSpanForSchedule(SignalLength)})";

            return $"{ChannelId},{timePart},{signalPart}";
        }

        /// <summary>
        /// Parses a string representation of a time span, which can be in HH:mm:ss format, or a number followed by 'h', 'm', or 's'.
        /// Also handles a special "*" character, which resolves to the current time.
        /// </summary>
        /// <param name="str">The string to parse. Examples: "10:30:00", "30s", "5m", "1h", "*".</param>
        /// <returns>A <see cref="TimeSpan"/> object if parsing is successful; otherwise, null.</returns>
        private static TimeSpan? TimeSpanParse(string? str)
        {
            TimeSpan? result = null;
            if (string.IsNullOrWhiteSpace(str))
            {
                return null;
            }

            str = str.Trim().ToLowerInvariant();
            string[]? colonParts = str.Split(':');

            if (string.IsNullOrWhiteSpace(str))
            {
                result = null;
            }
            else if (str == "*")
            {
                // Represents the current time of day
                result = DateTime.Now.TimeOfDay;
            }
            else if (colonParts != null && colonParts.Length == 2 && int.TryParse(colonParts[0], out int hours) && int.TryParse(colonParts[1], out int minutes))
            {
                result = new TimeSpan(hours, minutes, 0);
            }
            else if (colonParts != null && colonParts.Length == 3 && int.TryParse(colonParts[0], out hours) && int.TryParse(colonParts[1], out minutes) && int.TryParse(colonParts[2], out int seconds))
            {
                result = new TimeSpan(hours, minutes, seconds);
            }
            else if (colonParts != null && colonParts.Length == 1 && str.Length >= 2)
            {
                char unit = str.Last();
                if (double.TryParse(str[..^1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double number))
                {
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
            }

            // Fallback for standard TimeSpan string formats if specific parsing above failed
            if (result == null && TimeSpan.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, out TimeSpan parsedResult))
            {
                result = parsedResult;
            }

            return result;
        }

        /// <summary>
        /// Formats a TimeSpan into a string suitable for the schedule output, e.g., "30.5s", "2m", "1.25h".
        /// </summary>
        /// <param name="timeSpan">The TimeSpan to format.</param>
        /// <returns>A string representation of the TimeSpan.</returns>
        private static string FormatTimeSpanForSchedule(TimeSpan timeSpan)
        {
            if (timeSpan.TotalHours >= 1 && timeSpan.TotalMinutes % 60 == 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
            {
                return $"{timeSpan.TotalHours}h";
            }
            else if (timeSpan.TotalMinutes >= 1 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
            {
                return $"{timeSpan.TotalMinutes}m";
            }
            else if (timeSpan.Milliseconds == 0)
            {
                return $"{timeSpan.TotalSeconds}s";
            }
            // Use a format that includes fractional seconds if needed, e.g., "ss.fff" and remove trailing zeros and dot.
            return timeSpan.ToString(@"ss\.fff").TrimEnd('0').TrimEnd('.') + "s";
        }
    }
}