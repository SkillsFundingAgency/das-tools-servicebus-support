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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface ICosmosMessageService
    {
        Task CreateAsync(ErrorMessage msg);
        Task BulkCreateAsync(IEnumerable<ErrorMessage> messsages);        
        void Delete();
        Task<IEnumerable<ErrorMessage>> GetErrorMessagesAsync(string userId);
    }


    public class CosmosMessageService : ICosmosMessageService
    {
        private readonly ILogger<CosmosMessageService> _logger;
        private readonly IConfiguration _config;
        private CosmosClient client;
        private readonly string databaseName;

        public CosmosMessageService(CosmosClient cosmosClient, IConfiguration config, ILogger<CosmosMessageService> logger)
        {
            _config = config;
            _logger = logger;
            
            client = cosmosClient;
            databaseName = _config.GetValue<string>("CosmosDb:DatabaseName");
        }        


        public async Task CreateAsync(ErrorMessage msg)
        {            
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);
            ItemResponse<ErrorMessage> response = await container.CreateItemAsync(msg);

        }
        public async Task BulkCreateAsync(IEnumerable<ErrorMessage> messsages)
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
                tasks.Add(container.CreateItemStreamAsync(item, new PartitionKey("123456"),requestOptions)
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



        public void Delete()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ErrorMessage>> GetErrorMessagesAsync(string userId)
        {
            var sqlQuery = $"SELECT * FROM c WHERE c.userId = '{userId}'"; 
            
            Database database = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            Container container = await database.CreateContainerIfNotExistsAsync(
                "Session",
                "/userId",
                400);

            QueryDefinition queryDefinition = new QueryDefinition(sqlQuery);
            FeedIterator<ErrorMessage> queryFeedIterator = container.GetItemQueryIterator<ErrorMessage>(queryDefinition);
            
            List<ErrorMessage> messages = new List<ErrorMessage>();

            while (queryFeedIterator.HasMoreResults)
            {
                FeedResponse<ErrorMessage> currentResults = await queryFeedIterator.ReadNextAsync();

                foreach (ErrorMessage message in currentResults)
                {
                    messages.Add(message);
                }
                
            }

            return messages;                          
        }
    }
}
