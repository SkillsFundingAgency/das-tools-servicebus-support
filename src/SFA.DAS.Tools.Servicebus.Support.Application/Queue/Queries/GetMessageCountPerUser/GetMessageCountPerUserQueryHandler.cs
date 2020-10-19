using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessageCountPerUser
{
    public class GetMessageCountPerUserQueryHandler : IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse>
    {
        private readonly ICosmosMessageDbContext _cosmosDbContext;

        public GetMessageCountPerUserQueryHandler(ICosmosMessageDbContext cosmosDbContext)
        {
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<GetMessageCountPerUserQueryResponse> Handle(GetMessageCountPerUserQuery query)
        {
            var messageCountPerUser = await _cosmosDbContext.GetMessageCountPerUser();

            var result = new Dictionary<string, List<UserMessageCount>>();
            foreach(var count in messageCountPerUser)
            {
                if (result.ContainsKey(count.Queue))
                {
                    result[count.Queue].Add(count);
                }
                else
                {
                    result.Add(count.Queue, new List<UserMessageCount> { count });    
                }
            }

            return new GetMessageCountPerUserQueryResponse()
            {
                QueueMessageCount = result
            };
        }
    }
}
