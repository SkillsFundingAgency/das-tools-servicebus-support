using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages
{
    public class ReceiveQueueMessagesQueryHandler : IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>
    {
        private readonly IAsbService _asbService;

        public ReceiveQueueMessagesQueryHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<ReceiveQueueMessagesQueryResponse> Handle(ReceiveQueueMessagesQuery query)
        {
            var messages = await _asbService.ReceiveMessagesAsync(query.QueueName, query.Limit);

            return new ReceiveQueueMessagesQueryResponse()
            {
                Messages = messages
            };
        }
    }
}
