using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages
{
    public class ReceiveQueueMessagesQuery
    {
        public string QueueName { get; set; }
        public int Limit { get; set; }
    }
}
