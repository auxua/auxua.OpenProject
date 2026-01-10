using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;

namespace OpenProjectClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var config = new BaseConfig()
            {
                PersonalAccessToken = Settings.ApiKey,
                BaseUrl = Settings.ApiBaseUrl
            };

            var client = new auxua.OpenProject.OpenProjectClient(config);

            HalCollection<Project> projects = client.Projects.GetProjectsAsync().Result;

            Console.WriteLine(projects.Total);
            Console.WriteLine(projects);

            Console.WriteLine("fin.");
            Console.ReadLine();
                

        }
    }
}
