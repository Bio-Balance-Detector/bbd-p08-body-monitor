using System;

namespace Fitbit.Models
{
    public enum HeartRateResolution
    {
        fiveSeconds = 1,
        oneMinute = 2,
        [Obsolete("No longer supported by Fitbit.")]
        fifteenMinute = 3
    }
}
