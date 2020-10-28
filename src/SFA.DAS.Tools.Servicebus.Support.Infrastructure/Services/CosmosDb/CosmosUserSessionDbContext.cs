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
        private readonly ICosmosDbPolicies _policies;
        private const string MessageType = "session";

        public CosmosUserSessionDbContext(ICosmosInfrastructureService cosmosInfrastructure, ICosmosDbPolicies policies)
        {
            _cosmosInfrastructure = cosmosInfrastructure;
            _policies = policies;
        }

        public async Task<UserSession> UpsertUserSessionAsync(UserSession userSession)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            return await _policies.ResiliencePolicy.ExecuteAsync(() => container.UpsertItemAsync(userSession));
        }

        public async Task<UserSession> GetUserSessionAsync(string userId)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var queryFeedIterator = container.GetItemLinqQueryable<UserSession>()
                .Where(s => s.UserId == userId && s.Type == MessageType)
                .ToFeedIterator()
            ;

            var currentResults = await _policies.ResiliencePolicy.ExecuteAsync(() => queryFeedIterator.ReadNextAsync());

            return currentResults.FirstOrDefault();
        }

        public async Task DeleteUserSessionAsync(string id, string userId)
        {
            var container = await _cosmosInfrastructure.CreateContainer();

            await _policies.ResiliencePolicy.ExecuteAsync(() => container.DeleteItemAsync<UserSession>(id, new PartitionKey(userId)));
        }

        public async Task<IEnumerable<UserSession>> GetExpiredUserSessionsAsync()
        {
            var container = await _cosmosInfrastructure.CreateContainer();

            return await _policies.ResiliencePolicy.ExecuteAsync(() => IterateUserSessionResults(container.GetItemLinqQueryable<UserSession>()
                .Where(s => s.ExpiryDateUtc < DateTime.UtcNow)
                .ToFeedIterator()));
        }

        public async Task<IEnumerable<UserSession>> GetUserSessionsAsync()
        {
            var container = await _cosmosInfrastructure.CreateContainer();

            return await _policies.ResiliencePolicy.ExecuteAsync(() => IterateUserSessionResults(container.GetItemLinqQueryable<UserSession>()
                .Where(s => s.Type == MessageType)
                .ToFeedIterator()));
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
