using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderCloud.SDK;

namespace ImportXC.Services.Deleters
{
    public class CategoryProvider : IItemProvider<Category>
    {
        private readonly OrderCloudClient client;

        public CategoryProvider(OrderCloudClient client)
        {
            this.client = client;
        }

        public async Task<Tuple<IList<Category>, int>> GetAll(string catalogId, int page)
        {
            var listPage = await client.Categories.ListAsync(catalogId, pageSize: 100, page: page);
            return new Tuple<IList<Category>, int>(listPage.Items, listPage.Meta.TotalCount);
        }

        public async Task Delete(string catalogId, Category item)
        {
            Console.WriteLine($"Deleting Category '{item.ID}'");
            await client.Categories.DeleteAsync(catalogId, item.ID);
        }
    }
}