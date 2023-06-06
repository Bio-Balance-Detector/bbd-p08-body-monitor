using System;

namespace Fitbit.Api.Portable.Models
{
    public class SleepLogDateRange
    {
        public DateTime DateOfSleep { get; set; }
        /// <summary>
        /// Total time in bed in milliseconds
        /// </summary>
        public int Duration { get; set; }
        public int Efficiency { get; set; }
        public bool IsMainSleep { get; set; }
        public Levels Levels { get; set; }
        public long LogId { get; set; }
        public int MinutesAfterWakeup { get; set; }
        public int MinutesAsleep { get; set; }
        public int MinutesAwake { get; set; }
        public int MinutesToFallAsleep { get; set; }
        public string LogType { get; set; }
        /// <summary>
        /// LOCAL time of sleep start
        /// </summary>
        public DateTime StartTime { get; set; }
        /// <summary>
        /// Total time in bed in minutes
        /// </summary>
        public int TimeInBed { get; set; }
        public string Type { get; set; }

        public override string ToString()
        {
            return $"Sleep log for the LOCAL time range of {StartTime} and {StartTime.AddMilliseconds(Duration)}";
        }
    }
}