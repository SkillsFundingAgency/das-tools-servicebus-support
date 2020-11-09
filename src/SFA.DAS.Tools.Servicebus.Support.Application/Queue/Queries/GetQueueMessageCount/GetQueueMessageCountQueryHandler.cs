using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount
{
    public class GetQueueMessageCountQueryHandler : IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>
    {
        private readonly IAsbService _asbService;

        public GetQueueMessageCountQueryHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<GetQueueMessageCountQueryResponse> Handle(GetQueueMessageCountQuery query)
        {
            var count = await _asbService.GetQueueMessageCountAsync(query.QueueName);

            return new GetQueueMessageCountQueryResponse()
            {
                Count = count,
            };
        }
    }
}
