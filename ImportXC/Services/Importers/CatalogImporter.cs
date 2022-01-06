using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace ImportXC.Services.Importers
{
    public class CatalogImporter : IImporter
    {
        private readonly OrderCloudClient client;
        private readonly string importFolder;

        public CatalogImporter(OrderCloudClient client, string importFolder)
        {
            this.client = client;
            this.importFolder = importFolder;
        }

        public async Task Import()
        {
            Console.WriteLine("Importing catalogs");

            var catalogFileNames = Directory.EnumerateFiles(this.importFolder, "Catalog.*.json");
            var catalogs = new List<Catalog>();

            foreach (var catalogFileName in catalogFileNames)
            {
                JObject catalogsJson = JObject.Parse(File.ReadAllText(catalogFileName));
                foreach (var catalogJson in catalogsJson["$values"])
                {
                    var catalogId = catalogJson["FriendlyId"].ToString();
                    try
                    {
                        var catalog = await client.Catalogs.GetAsync(catalogId);

                    }
                    catch (OrderCloudException ex)
                    {
                        if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                        {
                            try
                            {
                                Console.WriteLine($"Creating catalog {catalogId}...");
                                var catalog = new Catalog
                                {
                                    ID = catalogId,
                                    Name = catalogJson["Name"].ToString(),
                                    Active = true,
                                    Description = catalogJson["DisplayName"].ToString()
                                };
                                catalogs.Add(catalog);
                            }
                            catch (OrderCloudException e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        }
                    }
                }
            }

            await Throttler.RunAsync(catalogs, 100, 20, catalog => client.Catalogs.CreateAsync(catalog));
        }
    }
}
