using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById
{
    public class GetMessagesByIdQueryHandler : IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public GetMessagesByIdQueryHandler(ICosmosDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }


        public async Task<GetMessagesByIdQueryResponse> Handle(GetMessagesByIdQuery query)
        {
            var messages = await _cosmosDbContext.GetQueueMessagesByIdAsync(query.UserId, query.Ids);

            return new GetMessagesByIdQueryResponse()
            {
                Messages = messages
            };
        }
    }
}
