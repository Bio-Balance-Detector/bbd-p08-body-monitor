using Newtonsoft.Json;
using System;

namespace Fitbit.Models
{
    public class DistanceStats
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }
    }
}