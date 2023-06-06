using Fitbit.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Fitbit.Api.Portable
{
    public class FitbitRequestException : FitbitException
    {
        public HttpResponseMessage Response { get; set; }

        public FitbitRequestException(HttpResponseMessage response, IEnumerable<ApiError> errors, string message = default(string), Exception innerEx = null)
            : base(message ?? $"Fitbit Request exception - Http Status Code: {(int)response.StatusCode} - see errors for more details.", errors, innerEx)
        {
            this.Response = response;
        }
    }
}