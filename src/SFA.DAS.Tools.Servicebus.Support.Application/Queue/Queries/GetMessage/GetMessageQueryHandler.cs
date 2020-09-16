using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage
{
    public class GetMessageQueryHandler : IQueryHandler<GetMessageQuery, GetMessageQueryResponse>
    {
        private readonly ICosmosDbContext _cosmosDbContext;

        public GetMessageQueryHandler(ICosmosDbContext cosmosDbContext)
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
