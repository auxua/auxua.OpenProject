using auxua.OpenProject.Authentication;
using auxua.OpenProject.Client;
using System;
using System.Net.Http;
using System.Reflection;

namespace auxua.OpenProject
{
    /// <summary>
    /// Client entry point for interacting with an OpenProject server API.
    /// 
    /// This class centralizes configuration of the underlying HTTP transport,
    /// authentication provider and exposes typed API surface objects such as
    /// `Projects`, `WorkPackages`, `Activities` and others.
    /// </summary>
    public class OpenProjectClient
    {

        /// <summary>
        /// Library version read from the executing assembly.
        /// </summary>
        internal static readonly string Version = Assembly
                                                    .GetExecutingAssembly()
                                                    .GetName()
                                                    .Version?
                                                    .ToString();

        /// <summary>
        /// The underlying <see cref="HttpClient"/> used to perform HTTP requests
        /// against the OpenProject server.
        /// </summary>
        private readonly HttpClient _http;

        /// <summary>
        /// Optional authentication provider used to add authentication information
        /// (for example a personal access token) to outgoing requests.
        /// </summary>
        private readonly IAuthProvider? _auth;

        /// <summary>
        /// API interface for project-related operations
        /// </summary>
        public ProjectsApi Projects { get; }

        /// <summary>
        /// API interface for work package operations
        /// </summary>
        public WorkPackagesApi WorkPackages { get; }

        /// <summary>
        /// Registry for custom field definitions discovered or used by the client
        /// </summary>
        public CustomFieldRegistry CustomFields { get; } = new CustomFieldRegistry();

        /// <summary>
        /// API interface for activity-related operations
        /// </summary>
        public ActivitiesApi Activities { get; }

        /// <summary>
        /// API interface for relations (WP-relations)
        /// </summary>
        public RelationsApi Relations { get; }

        /// <summary>
        /// API interface for news-related operations
        /// </summary>
        public NewsApi News { get; }

        /// <summary>
        /// API interface for user management operations
        /// </summary>
        public UsersApi Users { get; }

        /// <summary>
        /// API interface for time entry operations.
        /// </summary>
        public TimeEntriesApi TimeEntries { get; }

        /// <summary>
        /// Creates a new instance of <see cref="OpenProjectClient"/>.
        /// </summary>
        /// <param name="options">Configuration used to initialize HTTP transport, base URL, timeout, user-agent and optional personal access token.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null or contains an invalid <see cref="BaseConfig.BaseUrl"/> value.</exception>
        public OpenProjectClient(BaseConfig options)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            _http = new HttpClient(handler)
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = options.Timeout,

            };

            // Configure default request headers for the client
            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/hal+json");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);

            // If a personal access token is provided, create a PAT auth provider
            _auth = string.IsNullOrWhiteSpace(options.PersonalAccessToken)
                ? null
                : new PatAuthProvider(options.PersonalAccessToken);

            Projects = new ProjectsApi(_http, _auth);
            WorkPackages = new WorkPackagesApi(_http, _auth, CustomFields);
            Activities = new ActivitiesApi(_http, _auth);
            Relations = new RelationsApi(_http, _auth);
            News = new NewsApi(_http, _auth);
            Users = new UsersApi(_http, _auth);
            TimeEntries =  new TimeEntriesApi(_http, _auth);
        }
    }
}