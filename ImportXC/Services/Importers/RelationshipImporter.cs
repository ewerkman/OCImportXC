using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Dasync.Collections;
using Newtonsoft.Json.Linq;
using OrderCloud.SDK;
using Polly;

namespace ImportXC.Services.Importers
{
    public class RelationshipImporter : IImporter
    {
        private readonly OrderCloudClient client;
        private readonly string importFolder;

        public RelationshipImporter(OrderCloudClient client, string importFolder)
        {
            this.client = client;
            this.importFolder = importFolder;
        }

        public async Task Import()
        {
            Console.WriteLine("Importing Relationships");

            var createdCategoryRelationships = 0;

            var relationshipsFileNames =
                Directory.EnumerateFiles(this.importFolder + @"\Relationships", "Relationships.*.json");

            var tasks = new List<Task>();
            var bulkhead = Policy.BulkheadAsync(10, Int32.MaxValue);

            foreach (var relationshipFileName in relationshipsFileNames)
            {
                JObject relationshipsJson = JObject.Parse(File.ReadAllText(relationshipFileName));

                await relationshipsJson["$values"].ParallelForEachAsync(async relationshipJson =>
                {
                    var relationshipType = relationshipJson["RelationshipType"].ToString();
                    var sourceId = relationshipJson["SourceId"].ToString();
                    var targetIds = relationshipJson["TargetIds"]["$values"];

                    Task t = null;
                    switch (relationshipType)
                    {
                        case "CatalogToCategory":
                            break;

                        case "CategoryToCategory":
                            t = bulkhead.ExecuteAsync(async () =>
                            {
                                Console.WriteLine($"Creating category relationship between {sourceId} and {targetIds}");
                                await HandleCategoryToCategoryRelationship(sourceId, targetIds, client);
                            });
                            tasks.Add(t);
                            break;

                        case "CategoryToSellableItem":
                            t = bulkhead.ExecuteAsync(async () =>
                            {
                                Console.WriteLine(
                                    $"Creating sellable item relationship between {sourceId} and {targetIds}");
                                await HandleCategoryToSellableItemRelationship(sourceId, targetIds, client);
                            });

                            break;
                    }

                    if (t != null)
                    {
                        tasks.Add(t);
                    }
                });
            }

            await Task.WhenAll(tasks);
        }


        private async Task<int> HandleCategoryToCategoryRelationship(string sourceId, JToken targetIds,
            OrderCloudClient client)
        {
            var createdCategoryRelationships = 0;

            var idParts = sourceId.Split("-"); // Entity-Category-Habitat_Master-Refrigerators
            var catalogId = idParts[2];
            var parentCategoryId = $"{idParts[2]}-{idParts[3].Replace(" ", "_")}";
            foreach (var targetId in targetIds)
            {
                var childId = targetId.ToString().Replace("Entity-Category-", "").Replace(" ", "_");
                var categoryPatch = new PartialCategory() {ParentID = parentCategoryId};

                try
                {
                    await client.Categories.PatchAsync(catalogId, childId, categoryPatch);

                    createdCategoryRelationships++;
                }
                catch (OrderCloudException e)
                {
                    if (e.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                    {
                        Console.WriteLine($"Category {childId} does not exist.");
                    }
                    else
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                }
            }

            return createdCategoryRelationships;
        }

        private async Task<int> HandleCategoryToSellableItemRelationship(string sourceId, JToken targetIds,
            OrderCloudClient client)
        {
            var createdSellableItemRelations = 0;

            var idParts = sourceId.Split("-"); // Entity-Category-Habitat_Master-Refrigerators
            var catalogId = idParts[2];
            var parentCategoryId = $"{idParts[2]}-{idParts[3].Replace(" ", "_")}";
            foreach (var targetId in targetIds)
            {
                var childId = targetId.ToString().Replace("Entity-SellableItem-", "");

                var categoryProductAssignment = new CategoryProductAssignment()
                {
                    CategoryID = parentCategoryId,
                    ProductID = childId
                };

                try
                {
                    await client.Categories.SaveProductAssignmentAsync(catalogId, categoryProductAssignment);
                    createdSellableItemRelations++;
                }
                catch (OrderCloudException e)
                {
                    if (e.HttpStatus == HttpStatusCode.NotFound)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Product {childId} could not be found...");
                        Console.ResetColor();
                    }
                }
            }

            return createdSellableItemRelations;
        }
    }
}