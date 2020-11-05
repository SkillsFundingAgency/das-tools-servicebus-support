using SFA.DAS.Tools.Servicebus.Support.Audit.MessageBuilders;
using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class AuditService : IAuditService
    {
        private readonly IAuditApiClient _client;
        private readonly IEnumerable<IAuditMessageBuilder> _builders;

        public AuditService(IAuditApiClient client, IEnumerable<IAuditMessageBuilder> builders)
        {
            _client = client;
            _builders = builders;
        }

        public async Task WriteAudit(AuditMessage message)
        {
            foreach (var builder in _builders)
            {
                builder.Build(message);
            }

            await _client.Audit(message);
        }
    }
}
