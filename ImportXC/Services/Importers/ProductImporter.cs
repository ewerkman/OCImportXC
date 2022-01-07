using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dasync.Collections;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace ImportXC.Services.Importers
{
    public class ProductImporter : IImporter
    {
        private readonly OrderCloudClient client;
        private readonly string importFolder;
        
        public ProductImporter(OrderCloudClient client, string importFolder)
        {
            this.client = client;
            this.importFolder = importFolder;
        }
        
        public async Task Import()
        {
            Console.WriteLine("Importing Products");

            int found = 0;
            int created = 0;
            int existing = 0;
            
            var sellableItemsFileNames = Directory.EnumerateFiles(importFolder, "SellableItem.*.json");
            foreach (var sellableItemFileName in sellableItemsFileNames)
            {
                JObject sellableItemsJson = JObject.Parse(File.ReadAllText(sellableItemFileName));

                found += sellableItemsJson["$values"].Count();
                var products = new List<Product>();

                await sellableItemsJson["$values"].ParallelForEachAsync(async sellableItemJson =>
                {
                    var friendlyId = sellableItemJson["FriendlyId"].ToString();
                    var productId = friendlyId.Replace(" ", "_");

                    try
                    {
                        await client.Products.GetAsync(productId);
                        existing++;
                    }
                    catch (OrderCloudException ex)
                    {
                        if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                        {
                            try
                            {
                                var hasPrice = await CreatePriceSchedule(client, sellableItemJson, productId);
                                
                                var product = new Product
                                {
                                    ID = productId,
                                    Name = sellableItemJson["DisplayName"].ToString(),
                                    Active = true,
                                    Description = sellableItemJson["Description"].ToString()
                                };

                                if (hasPrice)
                                {
                                    product.DefaultPriceScheduleID = productId;
                                }
                                
                                product.xp.brand = sellableItemJson["Brand"].ToString();
                                product.xp.manufacturer = sellableItemJson["Manufacturer"].ToString();
                                
                                products.Add(product);

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

                await Throttler.RunAsync(products, 100, 20, p => client.Products.CreateAsync(p));
            }
            
            Console.WriteLine($"Products: Found: {found} - Created: {created} - Existing: {existing}");
        }

        private async Task<bool> CreatePriceSchedule(OrderCloudClient client, JToken sellableItemJson,
            string productId)
        {
            var listPricingPolicy = sellableItemJson["Policies"]["$values"].FirstOrDefault(p =>
                p["$type"].ToString() ==
                "Sitecore.Commerce.Plugin.Pricing.ListPricingPolicy, Sitecore.Commerce.Plugin.Pricing");
            if (listPricingPolicy != null)
            {
                try
                {
                    await client.PriceSchedules.GetAsync(productId);

                    return true;
                }
                catch (OrderCloudException ex)
                {
                    if (ex.HttpStatus == HttpStatusCode.NotFound) // Object does not exist
                    {
                        try
                        {
                            var prices = listPricingPolicy["Prices"]["$values"];
                            var priceBreaks = new List<PriceBreak>();

                            foreach (var price in prices)
                            {
                                priceBreaks.Add(new PriceBreak()
                                {
                                    Price = decimal.Parse(price["Amount"].ToString()),
                                    Quantity = 0
                                });
                            }

                            var priceSchedule = new PriceSchedule()
                            {
                                ID = productId,
                                Name = productId,
                                PriceBreaks = priceBreaks,
                                UseCumulativeQuantity = true
                            };

                            await client.PriceSchedules.CreateAsync(priceSchedule);

                            return true;
                        }
                        catch (OrderCloudException e)
                        {
                            Console.WriteLine(e);
                            //TODO: Write to a report for manual retry later?
                        }
                    }
                }
            }

            return false;
        }

        private async Task CreateVariations(OrderCloudClient client, JToken sellableItemJson, Product createdProduct)
        {
            var components = sellableItemJson["Components"]["$values"];
            var itemVariationsComponent = components.FirstOrDefault(c =>
                c["$type"].ToString() ==
                "Sitecore.Commerce.Plugin.Catalog.ItemVariationsComponent, Sitecore.Commerce.Plugin.Catalog");

            if (itemVariationsComponent != null)
            {
                var itemVariationComponents = itemVariationsComponent["ChildComponents"]["$values"].Where(c =>
                    c["$type"].ToString() ==
                    "Sitecore.Commerce.Plugin.Catalog.ItemVariationComponent, Sitecore.Commerce.Plugin.Catalog");
                foreach (var itemVariationComponent in itemVariationComponents)
                {
                    Console.WriteLine($"Creating variation {itemVariationComponent["Id"]}");

                    var variantId = itemVariationComponent["Id"].ToString();

                    var variant = new Variant()
                    {
                        ID = variantId,
                        Name = itemVariationComponent["DisplayName"].ToString(),
                        Active = true,
                        Description = itemVariationComponent["Description"].ToString()
                    };

                    await client.Products.GenerateVariantsAsync(createdProduct.ID);
                    await client.Products.SaveVariantAsync(createdProduct.ID, variantId, variant);
                }
            }
        }
    }
}
