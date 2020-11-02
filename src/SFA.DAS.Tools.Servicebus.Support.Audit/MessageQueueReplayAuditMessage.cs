using Newtonsoft.Json;
using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class MessageQueueReplayAuditMessage : AuditMessage
    {
        public MessageQueueReplayAuditMessage(QueueMessage message)
        {
            AffectedEntity = new Entity
            {
                Type = "QueueMessage",
                Id = message.Id
            };
            Category = "REPLAY_QUEUE_MESSAGE";
            Description = $"Replay queue message of id : {message.Id} with CorrelationId : {message.OriginalMessage.CorrelationId}";
            ChangedProperties = new List<PropertyUpdate>
            {
                PropertyUpdate.FromString("meessage", JsonConvert.SerializeObject(message.OriginalMessage?.UserProperties))
            };
        }
    }
}
