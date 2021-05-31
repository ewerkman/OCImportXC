using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderCloud.SDK;

namespace ImportXC.Services.Deleters
{
    public class ProductProvider : IItemProvider<Product>
    {
        private readonly OrderCloudClient client;

        public ProductProvider(OrderCloudClient client)
        {
            this.client = client;
        }

        public async Task<Tuple<IList<Product>, int>> GetAll(string catalogId, int page)
        {
            var listPage = await client.Products.ListAsync(pageSize: 100, page: page);
            return new Tuple<IList<Product>, int>(listPage.Items, listPage.Meta.TotalCount);
        }

        public async Task Delete(string catalogId, Product item)
        {
            Console.WriteLine($"\rDeleting Product '{item.ID}'");
            try
            {
                await client.Products.DeleteAsync(item.ID);
            }
            catch (OrderCloudException e)
            {
                Console.WriteLine(e);
            }
        }
    }
}