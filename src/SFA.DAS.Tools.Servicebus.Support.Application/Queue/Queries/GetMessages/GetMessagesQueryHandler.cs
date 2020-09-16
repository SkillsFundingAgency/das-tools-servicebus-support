using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages
{
    public class GetMessagesQueryHandler : IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public GetMessagesQueryHandler(ICosmosDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetMessagesQueryResponse> Handle(GetMessagesQuery query)
        {
            var messages = _cosmosDbContext.GetQueueMessagesAsync(query.UserId, query.SearchProperties);
            var unfilteredCnt = _cosmosDbContext.GetMessageCountAsync(query.UserId);
            var cnt = _cosmosDbContext.GetMessageCountAsync(query.UserId, new SearchProperties()
            {
                Search = query.SearchProperties.Search
            });

            await Task.WhenAll(messages, cnt, unfilteredCnt);

            return new GetMessagesQueryResponse()
            {
                Messages = await messages,
                Count = await cnt,
                UnfilteredCount = await unfilteredCnt
            };
        }
    }
}
