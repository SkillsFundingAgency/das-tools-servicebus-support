using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage
{
    public class GetMessageQuery
    {
        public string UserId { get; set; }
        public string MessageId { get; set; }
    }
}
