using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages
{
    public class PeekQueueMessagesQueryHandler : IQueryHandler<PeekQueueMessagesQuery, PeekQueueMessagesQueryResponse>
    {
        private readonly IAsbService _asbService;

        public PeekQueueMessagesQueryHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<PeekQueueMessagesQueryResponse> Handle(PeekQueueMessagesQuery query)
        {
            var messages = await _asbService.PeekMessagesAsync(query.QueueName);

            return new PeekQueueMessagesQueryResponse()
            {
                Messages = messages
            };
        }
    }
}
