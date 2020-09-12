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
            var cnt = _cosmosDbContext.GetUserMessageCountAsync(query.UserId);

            await Task.WhenAll(messages, cnt);

            return new GetMessagesQueryResponse()
            {
                Messages = await messages,
                Count = await cnt
            };
        }
    }
}
