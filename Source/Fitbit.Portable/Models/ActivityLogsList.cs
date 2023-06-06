using Newtonsoft.Json;
using System.Collections.Generic;

namespace Fitbit.Api.Portable.Models
{
    public class ActivityLogsList
    {
        [JsonProperty(PropertyName = "activities")]
        public List<Activities> Activities { get; set; }


    }
}
