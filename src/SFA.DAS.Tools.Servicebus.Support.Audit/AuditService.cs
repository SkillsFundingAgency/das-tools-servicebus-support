using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class AuditService : IAuditService
    {
        private readonly IAuditApiClient _client;
        private readonly IAuditMessageFactory _messageFactory;

        public AuditService(IAuditApiClient client, IAuditMessageFactory messageFactory)
        {
            _client = client;
            _messageFactory = messageFactory;
        }

        public async Task WriteAudit(AuditMessage message)
        {
            var auditMessage = _messageFactory.Build();
            auditMessage.AffectedEntity = message.AffectedEntity;
            auditMessage.Category = message.Category;
            auditMessage.Description = message.Description;
            auditMessage.ChangedProperties = message.ChangedProperties;
            auditMessage.RelatedEntities = message.RelatedEntities;

            await _client.Audit(auditMessage);
        }
    }
}
