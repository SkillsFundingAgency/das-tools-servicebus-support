﻿using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class CosmosDbContext : ICosmosDbContext
    {
        private readonly ILogger<CosmosDbContext> _logger;
        private readonly IConfiguration _config;
        private CosmosClient client;
        private readonly string databaseName;

        public CosmosDbContext(CosmosClient cosmosClient, IConfiguration config, ILogger<CosmosDbContext> logger)
        {
            _config = config;
            _logger = logger;
            
            client = cosmosClient;
            databaseName = _config.GetValue<string>("CosmosDb:DatabaseName");            
        }        


        public async Task CreateQueueMessageAsync(QueueMessage msg)
        {            
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);
            ItemResponse<QueueMessage> response = await container.CreateItemAsync(msg);

        }
        public async Task<BulkOperationResponse<QueueMessage>> BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messsages)
        {                                   
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);



            List<Task<OperationResponse<QueueMessage>>> tasks = new List<Task<OperationResponse<QueueMessage>>>();
            ItemRequestOptions requestOptions = new ItemRequestOptions() { EnableContentResponseOnWrite = false };

            foreach (var msg in messsages)
            {
                tasks.Add(container.CreateItemAsync(msg, new PartitionKey(UserService.GetUserId()), requestOptions).CaptureOperationResponse(msg));            
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            await Task.WhenAll(tasks);
            stopwatch.Stop();

            return new BulkOperationResponse<QueueMessage>()
            {
                TotalTimeTaken = stopwatch.Elapsed,
                TotalRequestUnitsConsumed = tasks.Sum(task => task.Result.RequestUnitsConsumed),
                SuccessfulDocuments = tasks.Count(task => task.Result.IsSuccessful),
                Failures = tasks.Where(task => !task.Result.IsSuccessful).Select(task => (task.Result.Item, task.Result.CosmosException)).ToList(),
                SuccessfulDocumentsLockTokens = tasks.Where(task => task.Result.IsSuccessful).Select(task=> task.Result.Item.OriginalMessage.SystemProperties.LockToken).ToList()
            };
        }



        public async Task DeleteQueueMessageAsync(QueueMessage msg)
        {
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);

            await container.DeleteItemAsync<QueueMessage>(msg.id.ToString(), new PartitionKey(msg.userId));
        }

        public async Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.userId = '{userId}'"; 
            
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
            FeedIterator<QueueMessage> queryFeedIterator = container.GetItemQueryIterator<QueueMessage>(queryDefinition);
            
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

        public async Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.userId = '{userId}' and c.id = '{messageId}'";

            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
            FeedIterator<QueueMessage> queryFeedIterator = container.GetItemQueryIterator<QueueMessage>(queryDefinition);
            FeedResponse<QueueMessage> currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.FirstOrDefault();
        }

        public async Task<int> GetUserMessageCountAsync(string userId)
        {
            var sqlQuery = $"SELECT value count(1) FROM c WHERE c.userId = '{userId}'";

            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
            FeedIterator<int> queryFeedIterator = container.GetItemQueryIterator<int>(queryDefinition);
            FeedResponse<int> currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.FirstOrDefault();
        }

        public async Task<bool> HasUserAnExistingSession(string userId) => await GetUserMessageCountAsync(userId) > 0;
    }
}