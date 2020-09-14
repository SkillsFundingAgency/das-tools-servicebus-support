using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class CosmosDbContext : ICosmosDbContext
    {
        private readonly ILogger<CosmosDbContext> _logger;
        private readonly CosmosClient _client;
        private readonly string _databaseName;
        private readonly IUserService _userService;

        public CosmosDbContext(CosmosClient cosmosClient, IUserService userService, IConfiguration config, ILogger<CosmosDbContext> logger)
        {
            _userService = userService;
            _logger = logger;
            _client = cosmosClient;
            _databaseName = config.GetValue<string>("CosmosDb:DatabaseName");
        }        

        public async Task CreateQueueMessageAsync(QueueMessage msg)
        {            
            Database database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await CrateContainer(database);
            await container.CreateItemAsync(msg);
        }

        public async Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messsages)
        {                                   
            Database database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await CrateContainer(database);

            TransactionalBatch batch = container.CreateTransactionalBatch(new PartitionKey(_userService.GetUserId()));
            
            foreach (var msg in messsages)
            {
                batch.CreateItem<QueueMessage>(msg);                
            }

            TransactionalBatchResponse batchResponse = await batch.ExecuteAsync();

            if (!batchResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Cosmos batch creation failed", batchResponse);
                throw new Exception("Cosmos batch creation failed");
            }
        }

        public async Task DeleteQueueMessageAsync(QueueMessage msg)
        {
            Database database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await CrateContainer(database);

            await container.DeleteItemAsync<QueueMessage>(msg.Id.ToString(), new PartitionKey(msg.UserId));
        }

        public async Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId, SearchProperties searchProperties)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.userId ='{userId}' {(searchProperties.Sort == null ? string.Empty : "ORDER BY c." + searchProperties.Sort + " " + searchProperties.Order)}  {(searchProperties.Offset == null ? "" : "OFFSET " + searchProperties.Offset)} {(searchProperties.Limit == null ? "" : "LIMIT " + searchProperties.Limit)}";
            
            var queryFeedIterator = await QuerySetup<QueueMessage>(sqlQuery);

            List<QueueMessage> messages = new List<QueueMessage>();

            while (queryFeedIterator.HasMoreResults)
            {
                FeedResponse<QueueMessage> currentResults = await queryFeedIterator.ReadNextAsync();

                foreach (QueueMessage message in currentResults)
                {
                    messages.Add(message);
                }                
            }

            return messages;                          
        }

        public async Task<int> GetUserMessageCountAsync(string userId)
        {
            var sqlQuery = $"SELECT VALUE COUNT(1) FROM c WHERE c.userId ='{userId}'";
            var queryFeedIterator = await QuerySetup<int>(sqlQuery);
            FeedResponse<int> currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.First();
        }
       
        public async Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.userId = '{userId}' and c.id = '{messageId}'";

            Database database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await CrateContainer(database);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
            FeedIterator<QueueMessage> queryFeedIterator = container.GetItemQueryIterator<QueueMessage>(queryDefinition);
            FeedResponse<QueueMessage> currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.FirstOrDefault();
        }
        public async Task<bool> HasUserAnExistingSession(string userId) => await GetUserMessageCountAsync(userId) > 0;

        private async Task<FeedIterator<T>> QuerySetup<T>(string sqlQuery)
        {
            Database database = await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
            var container = await CrateContainer(database);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
            var queryFeedIterator = container.GetItemQueryIterator<T>(queryDefinition);
            return queryFeedIterator;
        }

        private static async Task<Container> CrateContainer(Database database)
        {
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);
            
            return container;
        }
    }
}
