using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace ImportXC
{
    [Command(Name = "importxc", Description = "Sitecore XC Catalog Importer"),
     Subcommand(typeof(CleanCatalogCommand), typeof(ImportCommand))]
    class Program
    {
        public static Task<int> Main(string[] args)
        {
            var app = new CommandLineApplication<Program>();
            app.Conventions
                .UseDefaultConventions();
            return app.ExecuteAsync(args);
        }
        
        private async Task<int> OnExecuteAsync(CommandLineApplication app, CancellationToken cancellationToken = default)
        {
            app.ShowHint();
            return 0;
        }
    }
}