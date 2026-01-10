using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace auxua.OpenProject.Authentication
{
    public sealed class PatAuthProvider : IAuthProvider
    {
        private readonly string _basicValue;

        public PatAuthProvider(string personalAccessToken)
        {
            if (string.IsNullOrWhiteSpace(personalAccessToken))
                throw new ArgumentException("PAT must not be empty.", nameof(personalAccessToken));

            var raw = $"apikey:{personalAccessToken}";
            _basicValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
        }

        public void Apply(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _basicValue);
        }
    }
}
