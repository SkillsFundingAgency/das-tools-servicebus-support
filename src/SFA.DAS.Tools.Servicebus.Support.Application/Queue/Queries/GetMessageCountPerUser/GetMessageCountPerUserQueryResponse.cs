using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessageCountPerUser
{
    public class GetMessageCountPerUserQueryResponse
    {
        public Dictionary<string, List<UserMessageCount>> QueueMessageCount { get; set; }
    }
}
