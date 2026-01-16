using auxua.OpenProject.Authentication;
using auxua.OpenProject.Model;
using ObjectDumping;
using static auxua.OpenProject.Client.WorkPackagesApi;

namespace OpenProjectClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            var config = new BaseConfig()
            {
                PersonalAccessToken = Settings.ApiKey,
                BaseUrl = Settings.ApiBaseUrl
            };

            var testproject = "[API-Testing] Zeitplan";

            var client = new auxua.OpenProject.OpenProjectClient(config);

            //HalCollection<Project> projects = await client.Projects.GetProjectsAsync();

            //Console.WriteLine(projects.Total);
            //Console.WriteLine(projects.Elements.Count);

            //foreach (var p in projects.Elements)
            //    Console.WriteLine("\t" + p.Name);

            //var proj = projects.Elements.Where(x => x.Name == testporject);
            ////Console.WriteLine(proj.Dump());

            var allproj = await client.Projects.GetAllProjectsAsync();
            var proj = allproj.Where(x => x.Name == testproject);
            var id = proj.First().Id;


            //foreach (var p in allproj)
            //    Console.WriteLine("\t" + p.Name);

            var query = WorkPackageQuery.ForProject(id);

            var wp = await client.WorkPackages.GetWorkPackagesAsync(query);
            var wp2 = await client.WorkPackages.GetAllWorkPackagesAsync(query);

            var koop = wp2.Where(x => x.Subject.Contains("KOOP (NaIon)")).First();


            List<string> cfields = new List<string>();
            foreach (var item in wp2)
            {
                var fields = item.Links.Where(x => x.Key.StartsWith("customField"));
                foreach (var c in fields)
                    cfields.Add(c.Key);
            }

            var fkoop = new WorkPackageFacade(koop, client.CustomFields);

            Console.WriteLine(fkoop.GetCustomFields().Dump());

            //Console.WriteLine(koop.Dump());

            Console.WriteLine("fin.");
            Console.ReadLine();
                

        }
    }
}
