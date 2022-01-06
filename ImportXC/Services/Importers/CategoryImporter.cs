using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dasync.Collections;
using ImportXC.Models;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace ImportXC.Services.Importers
{
    public class CategoryImporter : IImporter
    {
        private readonly OrderCloudClient client;
        private readonly string importFolder;

        public CategoryImporter(OrderCloudClient client, string importFolder)
        {
            this.client = client;
            this.importFolder = importFolder;
        }
        
        public async Task Import()
        {
            Console.WriteLine("Importing categories...");

            int found = 0;
            int created = 0;
            int existing = 0;

            var newCategories = new List<NewCategory>();

            var categoryFileNames = Directory.EnumerateFiles(importFolder, "Category.*.json");
            foreach (var categoryFileName in categoryFileNames)
            {
                JObject categoriesJson = JObject.Parse(File.ReadAllText(categoryFileName));

                found += categoriesJson["$values"].Count();

                await categoriesJson["$values"].ParallelForEachAsync(async categoryJson =>
                {
                    var friendlyId = categoryJson["FriendlyId"].ToString();
                    var idParts = friendlyId.Split("-");

                    var catalogId = idParts[0];
                    var categoryId = friendlyId.Replace(" ", "_");

                    try
                    {
                        var category = await client.Categories.GetAsync(catalogId, categoryId);
                        existing++;
                    }
                    catch (OrderCloudException ex)
                    {
                        if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                        {
                            try
                            {
                                var category = new Category
                                {
                                    ID = categoryId,
                                    Name = categoryJson["DisplayName"].ToString(),
                                    Active = true,
                                    Description = categoryJson["Description"].ToString()
                                };
                                newCategories.Add(new NewCategory()
                                    {CatalogId = catalogId, Category = category});

                                created++;
                            }
                            catch (OrderCloudException e)
                            {
                                Console.WriteLine(e);
                                throw;
                            }
                        }
                    }
                });
            }

            await Throttler.RunAsync(newCategories, 100, 20,
                category => client.Categories.CreateAsync(category.CatalogId, category.Category));

            Console.WriteLine($"Categories: Found: {found} - Created: {created} - Existing: {existing}");
        }

    }
}
