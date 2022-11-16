using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    internal class SecureHttpClient
    {
        private readonly IAuditApiConfiguration _configuration;
        public SecureHttpClient(IAuditApiConfiguration configuration)
        {
            _configuration = configuration;
        }
        protected SecureHttpClient()
        {
            // So we can mock for testing
        }
        public virtual async Task PostAsync(string url, AuditMessage message)
        {
            var accessToken = await GetAuthenticationToken();

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Add("api-version", "1");
                client.DefaultRequestHeaders.Add("accept", "application/json");

                var response = await client.PostAsync(url, new StringContent(JsonConvert.SerializeObject(message), Encoding.UTF8, "application/json"));                
            }
        }
        private async Task<string> GetAuthenticationToken()
        {
            var accessToken = IsClientCredentialConfiguration(_configuration.ClientId, _configuration.ClientSecret, _configuration.Tenant)
               ? await GetClientCredentialAuthenticationResult(_configuration.ClientId, _configuration.ClientSecret, _configuration.IdentifierUri, _configuration.Tenant)
               : await GetManagedIdentityAuthenticationResult(_configuration.IdentifierUri);

            return accessToken;
        }
        private static bool IsClientCredentialConfiguration(string clientId, string clientSecret, string tenant)
        {
            return !string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret) && !string.IsNullOrEmpty(tenant);
        }
        private static async Task<string> GetClientCredentialAuthenticationResult(string clientId, string clientSecret, string resource, string tenant)
        {
            var authority = $"https://login.microsoftonline.com/{tenant}";
            var clientCredential = new ClientCredential(clientId, clientSecret);
            var context = new AuthenticationContext(authority, true);
            var result = await context.AcquireTokenAsync(resource, clientCredential);
            return result.AccessToken;
        }
        private static async Task<string> GetManagedIdentityAuthenticationResult(string resource)
        {
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            return await azureServiceTokenProvider.GetAccessTokenAsync(resource);
        }
    }
}
