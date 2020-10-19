using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosUserSessionDbContext : ICosmosUserSessionDbContext
    {
        private readonly CosmosClient _client;
        private readonly ICosmosInfrastructureService _cosmosInfrastructure;
        private readonly string _databaseName;

        public CosmosUserSessionDbContext(CosmosClient cosmosClient, ICosmosInfrastructureService cosmosInfrastructure, IConfiguration config)
        {
            _client = cosmosClient;            
            _cosmosInfrastructure = cosmosInfrastructure;
            _databaseName = config.GetValue<string>("CosmosDb:DatabaseName");
        }

        public async Task<UserSession> UpsertUserSessionAsync(UserSession userSession)
        {
            var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await _cosmosInfrastructure.CreateContainer(database);
            return await container.UpsertItemAsync(userSession);
        }

        public async Task<UserSession> GetUserSessionAsync(string userId)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.userId = '{userId}' and c.type = 'session'";

            var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await _cosmosInfrastructure.CreateContainer(database);

            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryFeedIterator = container.GetItemQueryIterator<UserSession>(queryDefinition);
            var currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.FirstOrDefault();
        }

        public async Task DeleteUserSessionAsync(string id, string userId)
        {
            Database database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await _cosmosInfrastructure.CreateContainer(database);

            await container.DeleteItemAsync<UserSession>(id, new PartitionKey(userId));
        }

        public async Task<IEnumerable<UserSession>> GetExpiredUserSessionsAsync()
        {
            var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await _cosmosInfrastructure.CreateContainer(database);

            var queryFeedIterator = container.GetItemLinqQueryable<UserSession>().Where(s => s.ExpiryDateUtc < DateTime.UtcNow).ToFeedIterator();

            var userSessions = new List<UserSession>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await queryFeedIterator.ReadNextAsync();

                var newSessions = currentResults.ToList();
                userSessions.AddRange(newSessions);
            }

            return userSessions;
        }

        public async Task<IEnumerable<UserSession>> GetUserSessionsAsync()
        {
            var sqlQuery = $"select * from c where c.type = 'session'";

            var database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await _cosmosInfrastructure.CreateContainer(database);

            var queryDefinition = new QueryDefinition(sqlQuery);
            var queryFeedIterator = container.GetItemQueryIterator<UserSession>(queryDefinition);
            
            var sessions = new List<UserSession>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await queryFeedIterator.ReadNextAsync();

                foreach (var session in currentResults)
                {
                    sessions.Add(session);
                }
            }

            return sessions;
        }
    }
}
