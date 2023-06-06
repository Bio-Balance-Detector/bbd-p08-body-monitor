using Newtonsoft.Json;
using System;

namespace Fitbit.Models
{
    public class FloorStats
    {
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }
    }
}