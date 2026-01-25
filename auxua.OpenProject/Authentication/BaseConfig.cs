using System;

namespace auxua.OpenProject.Authentication
{
    public class BaseConfig
    {
        /// <summary>
        /// The base URL of the OpenProject API.
        /// </summary>
        public string BaseUrl { get; set; } = "";

        /// <summary>
        /// Gets or sets the API key used to authenticate requests.
        /// </summary>
        public string PersonalAccessToken { get; set; } = "";

        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100);
        public string UserAgent { get; set; } = "OpenProjectClient/" + OpenProjectClient.Version;
    }
}