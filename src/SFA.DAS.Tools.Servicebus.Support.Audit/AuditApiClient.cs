using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class AuditApiClient : IAuditApiClient
    {
        private readonly SecureHttpClient _httpClient;
        private readonly IAuditApiConfiguration _configuration;

        public AuditApiClient(IAuditApiConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }
            if (string.IsNullOrEmpty(configuration.ApiBaseUrl))
            {
                throw new NullReferenceException("configuration.ApiBaseUrl is not specified");
            }
            if (string.IsNullOrEmpty(configuration.IdentifierUri))
            {
                throw new NullReferenceException("configuration.IdentifierUri is not specified");
            }

            _configuration = configuration;
            _httpClient = new SecureHttpClient(configuration);
        }

        internal AuditApiClient(IAuditApiConfiguration configuration, SecureHttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task Audit(AuditMessage message)
        {
            var baseUrl = _configuration.ApiBaseUrl.EndsWith("/")
                ? _configuration.ApiBaseUrl
                : _configuration.ApiBaseUrl + "/";
            var url = $"{baseUrl}api/audit";

            await _httpClient.PostAsync(url, message);
        }
    }
}
