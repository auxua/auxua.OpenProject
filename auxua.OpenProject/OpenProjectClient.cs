using auxua.OpenProject.Authentication;
using auxua.OpenProject.Client;
using System;
using System.Net.Http;
using System.Reflection;

namespace auxua.OpenProject
{
    public class OpenProjectClient
    {
        internal static readonly string Version = Assembly
                                                    .GetExecutingAssembly()
                                                    .GetName()
                                                    .Version?
                                                    .ToString();

        private readonly HttpClient _http;
        private readonly IAuthProvider? _auth;

        public ProjectsApi Projects { get; }
        public WorkPackagesApi WorkPackages { get; }
        public CustomFieldRegistry CustomFields { get; } = new CustomFieldRegistry();

        public OpenProjectClient(BaseConfig options)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            _http = new HttpClient(handler);
            _http = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = options.Timeout,
                
            };

            _http.DefaultRequestHeaders.Accept.Clear();
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/hal+json");
            _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            _http.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            

            _auth = string.IsNullOrWhiteSpace(options.PersonalAccessToken)
                ? null
                : new PatAuthProvider(options.PersonalAccessToken);

            
           

            Projects = new ProjectsApi(_http, _auth);
            WorkPackages = new WorkPackagesApi(_http, _auth, CustomFields);
        }
    }
}
