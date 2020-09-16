using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages
{
    public class GetMessagesQuery
    {
        public string UserId { get; set; }
        public SearchProperties SearchProperties { get; set; }
    }
}
