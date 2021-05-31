using ImportXC.Helpers;
using McMaster.Extensions.CommandLineUtils;
using OrderCloud.SDK;

namespace ImportXC
{
    public class OCCommand
    {
        [System.ComponentModel.DataAnnotations.Required]
        [Option(Description = "Client Id for OrderCloud.", LongName = "clientid", ShortName = "ci")]
        public string ClientId { get; } 
        
        [Option(CommandOptionType.SingleValue, Description = "Client secret for OrderCloud.", ShortName = "cs", LongName = "secret")]
        public string ClientSecret { get; }
        
        [System.ComponentModel.DataAnnotations.Required]
        [Option(Description = "Base url for OrderCloud calls. Default: https://sandboxapi.ordercloud.io")] 
        public string ApiUrl { get; } = "https://sandboxapi.ordercloud.io";

        protected OrderCloudClient GetOrderCloudClient()
        {
            return OrderCloudHelpers.GetOrderCloudClient(this.ClientId, this.ClientSecret, this.ApiUrl, this.ApiUrl);
        }
        
    }
}