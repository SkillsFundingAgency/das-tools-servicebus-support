using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues
{
    public class GetQueuesQueryHandler : IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse>
    {
        private readonly IAsbService _asbService;
        private readonly string _regexString;

        public GetQueuesQueryHandler(IAsbService asbService, ServiceBusErrorManagementSettings serviceBusSettings)
        {
            _regexString = serviceBusSettings.QueueSelectionRegex;
            _asbService = asbService;
        }

        public async Task<GetQueuesQueryResponse> Handle(GetQueuesQuery query)
        {
            List<QueueInfo> errorQueues = new List<QueueInfo>();

            bool hasNext = true;
            int skipCount = 0;
            while (hasNext)
            {
                var queueGetCount = 100;

                var queues = await _asbService.GetMessageQueuesAsync(skipCount, queueGetCount);

                var queueSelectionRegex = new Regex(_regexString);

                errorQueues.AddRange(queues.Where(q => queueSelectionRegex.IsMatch(q.Name)));

                hasNext = queues.Count() == queueGetCount;
                skipCount += queueGetCount;
            }


            return new GetQueuesQueryResponse()
            {
                Queues = errorQueues
            };
        }
    }
}