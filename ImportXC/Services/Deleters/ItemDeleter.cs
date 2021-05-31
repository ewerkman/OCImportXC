using System;
using System.Threading.Tasks;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace ImportXC.Services.Deleters
{
    public class ItemDeleter
    {
        public async Task Delete<T>(string catalogId, IItemProvider<T> itemProvider) where T : OrderCloudModel
        {
            try
            {
                var itemsLeftToDeleteCount = 0;
                var page = 1;
                do
                {
                    var result = await itemProvider.GetAll(catalogId, page);

                    var items = result.Item1;
                    itemsLeftToDeleteCount = result.Item2;

                    await Throttler.RunAsync(items, 100, 20, item => { return itemProvider.Delete(catalogId, item); });

                    //page++;
                } while (itemsLeftToDeleteCount > 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}