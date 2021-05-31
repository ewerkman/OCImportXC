using OrderCloud.SDK;

namespace ImportXC.Helpers
{
    public class OrderCloudHelpers
    {
        public static OrderCloudClient GetOrderCloudClient(string clientId, string clientSecret, string apiUrl,
            string authUrl)
        {
            return new OrderCloudClient(new OrderCloudClientConfig
            {
                ClientId = clientId,
                ClientSecret = clientSecret,
                Roles = new[] {ApiRole.FullAccess},
                ApiUrl = apiUrl,
                AuthUrl = authUrl
            });
        }
    }
}