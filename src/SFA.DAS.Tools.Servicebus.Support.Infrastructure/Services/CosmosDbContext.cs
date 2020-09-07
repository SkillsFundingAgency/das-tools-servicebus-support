using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using SFA.DAS.Tools.Servicebus.Support.Core;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface ICosmosDbContext
    {
        Task CreateQueueMessageAsync(QueueMessage msg);
        Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messsages);        
        Task DeleteQueueMessageAsync(QueueMessage msg);
        Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId);
        Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId);
        Task<int> GetUserMessageCountAsync(string userId);
    }

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
        public async Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messsages)
        {                                   
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);

            List<Stream> itemsToInsert = new List<Stream>();
            var serializeOptions = new JsonSerializerOptions();
            serializeOptions.Converters.Add(new TimeSpanToStringConverter());

            foreach (var msg in messsages)
            {
                MemoryStream stream = new MemoryStream();
                await JsonSerializer.SerializeAsync(stream, msg,serializeOptions);
                itemsToInsert.Add(stream);
            }

            List<Task> tasks = new List<Task>();
            ItemRequestOptions requestOptions = new ItemRequestOptions() { EnableContentResponseOnWrite = false };

            foreach (var item in itemsToInsert)
            {
                tasks.Add(container.CreateItemStreamAsync(item, new PartitionKey(UserService.GetUserId()),requestOptions)
                    .ContinueWith((Task<ResponseMessage> task) =>
                    {
                        using (ResponseMessage response = task.Result)
                        {
                            if (!response.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Received {response.StatusCode} ({response.ErrorMessage}).");
                            }
                        }
                    }));
            }

            await Task.WhenAll(tasks);            
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
    }
}
