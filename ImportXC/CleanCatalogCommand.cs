using System.Threading;
using System.Threading.Tasks;
using ImportXC.Helpers;
using ImportXC.Services.Deleters;
using McMaster.Extensions.CommandLineUtils;
using OrderCloud.Catalyst;
using OrderCloud.SDK;

namespace ImportXC
{
    [Command("clean", Description = "Clean up the catalog")]
    public class CleanCatalogCommand : OCCommand
    {
        [Option(ShortName = "sc", Description = "Skip category deletion")]
        public bool SkipCategory { get; } = false;
        
        [Option(LongName = "catalogid", ShortName = "ca", Description = "Id of catalog to clean.")]
        public string CatalogId { get; }

        private async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken = default)
        {
            var client = GetOrderCloudClient();

            var catalogId = this.CatalogId;

            var itemDeleter = new ItemDeleter();

            await itemDeleter.Delete<PriceSchedule>(catalogId, new PriceScheduleProvider(client));
            
            await itemDeleter.Delete<Product>(catalogId, new ProductProvider(client));
            
            if (!SkipCategory)
            {
                await itemDeleter.Delete<Category>(catalogId, new CategoryProvider(client));
            }

            await client.Catalogs.DeleteAsync(catalogId);
        }
    }
}