using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Domain.Queue
{
    public class ReceiveMessagesResponse
    {
        public IEnumerable<QueueMessage> Messages { get; set; }
    }
}
