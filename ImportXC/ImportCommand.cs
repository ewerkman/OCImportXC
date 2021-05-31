using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Dasync.Collections;
using ImportXC.Services.Importers;
using McMaster.Extensions.CommandLineUtils;
using Newtonsoft.Json.Linq;
using OrderCloud.Catalyst;
using OrderCloud.SDK;
using Polly;

namespace ImportXC
{
    [Command(Description = "Import a catalog")]
    public class ImportCommand : OCCommand
    {
        [Option(ShortName = "sc", Description = "Skip category import")]
        public bool SkipCategory { get; } = false;
        
        [Required]
        [Option(ShortName = "f", LongName = "importfolder", Description = "Folder containing Sitecore XC catalog JSON files.")]
        public string ImportFolder { get; }
        
        private async Task OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken = default)
        {
            var client = GetOrderCloudClient();
            
            await new CatalogImporter(client, ImportFolder).Import();

            if (!SkipCategory)
            {
                await new CategoryImporter(client, ImportFolder).Import();
            }

            await new ProductImporter(client, ImportFolder).Import();  

            await new RelationshipImporter(client, ImportFolder).Import();
        }

    }
}