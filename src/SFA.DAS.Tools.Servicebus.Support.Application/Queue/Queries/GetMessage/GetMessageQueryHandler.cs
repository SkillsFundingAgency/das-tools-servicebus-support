using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage
{
    public class GetMessageQueryHandler : IQueryHandler<GetMessageQuery, GetMessageQueryResponse>
    {
        private readonly ICosmosMessageDbContext _cosmosDbContext;

        public GetMessageQueryHandler(ICosmosMessageDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetMessageQueryResponse> Handle(GetMessageQuery query)
        {
            var message = await _cosmosDbContext.GetQueueMessageAsync(query.UserId, query.MessageId);

            return new GetMessageQueryResponse()
            {
                Message = message,
            };
        }
    }
}
