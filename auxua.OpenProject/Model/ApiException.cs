using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace auxua.OpenProject.Model
{
    public sealed class ApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string ResponseBody { get; }

        public ApiException(HttpStatusCode statusCode, string responseBody, string? message = null)
            : base(message ?? $"API request failed with {(int)statusCode} ({statusCode}).")
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
