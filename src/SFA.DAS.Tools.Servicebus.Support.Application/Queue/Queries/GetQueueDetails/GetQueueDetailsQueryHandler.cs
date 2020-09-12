using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails
{
    public class GetQueueDetailsQueryHandler : IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse>
    {
        private readonly IAsbService _asbService;

        public GetQueueDetailsQueryHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<GetQueueDetailsQueryResponse> Handle(GetQueueDetailsQuery query)
        {
            var queueInfo = await _asbService.GetQueueDetailsAsync(query.QueueName);

            return new GetQueueDetailsQueryResponse()
            {
                QueueInfo = queueInfo
            };
        }
    }
}
