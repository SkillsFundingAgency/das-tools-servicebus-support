using Newtonsoft.Json;
using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class MessageQueueDeleteAuditMessage : AuditMessage
    {
        public MessageQueueDeleteAuditMessage(IEnumerable<string> ids)
        {
            AffectedEntity = new Entity
            {
                Type = "QueueMessages",
                Id = Guid.NewGuid().ToString()
            };
            Category = "DELETE_QUEUE_MESSAGES";
            Description = $"Delete queue messages";
            ChangedProperties = new List<PropertyUpdate>
            {
                PropertyUpdate.FromString("ids", JsonConvert.SerializeObject(ids))
            };
        }
    }
}
