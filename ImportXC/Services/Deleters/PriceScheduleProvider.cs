using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderCloud.SDK;

namespace ImportXC.Services.Deleters
{
    public class PriceScheduleProvider : IItemProvider<PriceSchedule>
    {
        private readonly OrderCloudClient client;

        public PriceScheduleProvider(OrderCloudClient client)
        {
            this.client = client;
        }

        public async Task<Tuple<IList<PriceSchedule>, int>> GetAll(string catalogId, int page)
        {
            var listPage = await client.PriceSchedules.ListAsync(page: page, pageSize: 100);
            return new Tuple<IList<PriceSchedule>, int>(listPage.Items, listPage.Meta.TotalCount);
        }

        public async Task Delete(string catalogId, PriceSchedule item)
        {
            Console.WriteLine($"Deleting PriceSchedule '{item.ID}'");
            await client.PriceSchedules.DeleteAsync(item.ID);
        }
    }
}