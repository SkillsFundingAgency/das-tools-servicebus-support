using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues
{
    public class GetQueuesQueryHandler : IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse>
    {
        private readonly IAsbService _asbService;

        public GetQueuesQueryHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<GetQueuesQueryResponse> Handle(GetQueuesQuery query)
        {
            var queues = await _asbService.GetErrorMessageQueuesAsync();

            return new GetQueuesQueryResponse()
            {
                Queues = queues
            };
        }
    }
}
