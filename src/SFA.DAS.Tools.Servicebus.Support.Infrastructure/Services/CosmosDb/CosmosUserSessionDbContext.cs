using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosUserSessionDbContext : ICosmosUserSessionDbContext
    {
        private readonly ICosmosInfrastructureService _cosmosInfrastructure;
        private const string MessageType = "session";

        public CosmosUserSessionDbContext(ICosmosInfrastructureService cosmosInfrastructure)
        {
            _cosmosInfrastructure = cosmosInfrastructure;
        }

        public async Task<UserSession> UpsertUserSessionAsync(UserSession userSession)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            return await container.UpsertItemAsync(userSession);
        }

        public async Task<UserSession> GetUserSessionAsync(string userId)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var queryFeedIterator = container.GetItemLinqQueryable<UserSession>()
                .Where(s => s.UserId == userId && s.Type == MessageType)
                .ToFeedIterator()
            ;

            var currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.FirstOrDefault();
        }

        public async Task DeleteUserSessionAsync(string id, string userId)
        {
            var container = await _cosmosInfrastructure.CreateContainer();

            await container.DeleteItemAsync<UserSession>(id, new PartitionKey(userId));
        }

        public async Task<IEnumerable<UserSession>> GetExpiredUserSessionsAsync()
        {
            var container = await _cosmosInfrastructure.CreateContainer();

            return await IterateUserSessionResults(container.GetItemLinqQueryable<UserSession>()
                .Where(s => s.ExpiryDateUtc < DateTime.UtcNow)
                .ToFeedIterator());
        }

        public async Task<IEnumerable<UserSession>> GetUserSessionsAsync()
        {
            var container = await _cosmosInfrastructure.CreateContainer();

            return await IterateUserSessionResults(container.GetItemLinqQueryable<UserSession>()
                .Where(s => s.Type == MessageType)
                .ToFeedIterator());
        }

        private static async Task<IEnumerable<UserSession>> IterateUserSessionResults(FeedIterator<UserSession> queryFeedIterator)
        {
            var sessions = new List<UserSession>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await queryFeedIterator.ReadNextAsync();

                sessions.AddRange(currentResults.ToList());
            }

            return sessions;
        }
    }
}
