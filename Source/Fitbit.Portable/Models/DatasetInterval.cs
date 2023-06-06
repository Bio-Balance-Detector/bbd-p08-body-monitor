using System;

namespace Fitbit.Api.Portable.Models
{
    public class DatasetInterval
    {
        public DateTime TimeUtc { get; set; }
        public int Value { get; set; }
        public override string ToString()
        {
            return $"{Value} @ {TimeUtc}";
        }
    }
}
