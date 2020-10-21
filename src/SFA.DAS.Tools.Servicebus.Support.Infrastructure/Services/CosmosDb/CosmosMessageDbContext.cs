using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Exceptions;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosMessageDbContext : ICosmosMessageDbContext
    {
        private readonly ILogger<CosmosMessageDbContext> _logger;
        private readonly ICosmosInfrastructureService _cosmosInfrastructure;
        private readonly IUserService _userService;
        private static readonly string[] SearchFields = new string[] { "body", "processingEndpoint", "originatingEndpoint", "exception", "exceptionType" };

        public CosmosMessageDbContext(IUserService userService, ILogger<CosmosMessageDbContext> logger, ICosmosInfrastructureService cosmosInfrastructure)
        {
            _userService = userService;
            _logger = logger;
            _cosmosInfrastructure = cosmosInfrastructure;
        }

        public async Task CreateQueueMessageAsync(QueueMessage msg)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            await container.CreateItemAsync(msg);
        }

        public async Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messages)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var batch = container.CreateTransactionalBatch(new PartitionKey(_userService.GetUserId()));

            foreach (var msg in messages)
            {
                batch.CreateItem(msg);
            }

            var batchResponse = await batch.ExecuteAsync();

            if (!batchResponse.IsSuccessStatusCode)
            {
                // Do we need to handle failures due to duplicate messages in the batch.
                _logger.LogError("Cosmos batch creation failed, {0}", batchResponse.StatusCode);
                throw new CosmosBatchInsertException("Cosmos batch creation failed", batchResponse.StatusCode);
            }
        }

        public async Task DeleteQueueMessagesAsync(IEnumerable<string> ids)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var batch = container.CreateTransactionalBatch(new PartitionKey(_userService.GetUserId()));

            foreach (var id in ids)
            {
                batch.DeleteItem(id);
            }

            var batchResponse = await batch.ExecuteAsync();

            if (!batchResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Cosmos batch deletion failed: {batchResponse.ErrorMessage}");
                throw new Exception("Cosmos batch deletion failed");
            }
        }

        public async Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId, SearchProperties searchProperties)
        {
            var sqlQuery = AddTypeClause($"SELECT * FROM c WHERE c.userId = @userId");

            if (searchProperties != null)
            {
                sqlQuery = AddSearch(sqlQuery, searchProperties);
            }

            sqlQuery = AddOrderBy(sqlQuery, searchProperties);
            sqlQuery = AddPaging(sqlQuery, searchProperties);

            var queryDefinition = new QueryDefinition(sqlQuery)
                .WithParameter("@userId", userId);

            if (searchProperties != null)
            {
                foreach (var field in SearchFields)
                {
                    queryDefinition.WithParameter($"@{field}", searchProperties.Search);
                }
            }

            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<QueueMessage>(queryDefinition);
            var messages = new List<QueueMessage>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await queryFeedIterator.ReadNextAsync();

                messages.AddRange(currentResults.ToList());
            }

            return messages;
        }

        public async Task<IEnumerable<QueueMessage>> GetQueueMessagesByIdAsync(string userId, IEnumerable<string> ids)
        {
            var idsList = "\"" + string.Join($"\",\"", ids.Select(x => x.ToString()).ToArray()) + "\"";
            var sqlQuery = $"SELECT * FROM c WHERE c.userId ='{userId}' and c.id in ({idsList})";
            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<QueueMessage>(new QueryDefinition(sqlQuery));

            var messages = new List<QueueMessage>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await queryFeedIterator.ReadNextAsync();

                var newMessages = currentResults.ToList();
                messages.AddRange(newMessages);
            }

            return messages;
        }

        public async Task<int> GetMessageCountAsync(string userId, SearchProperties searchProperties = null)
        {
            var sqlQuery = AddTypeClause($"SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId");

            if (searchProperties != null)
            {
                sqlQuery = AddSearch(sqlQuery, searchProperties);
            }

            var queryDefinition = new QueryDefinition(sqlQuery)
                    .WithParameter("@userId", userId);

            if (searchProperties != null)
            {
                foreach (var field in SearchFields)
                {
                    queryDefinition.WithParameter($"@{field}", searchProperties.Search);
                }
            }

            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<int>(queryDefinition);
            var currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.First();
        }

        public async Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId)
        {
            var queryDefinition = new QueryDefinition(AddTypeClause($"SELECT * FROM c WHERE c.userId = @userId AND c.id = @messageId"))
                    .WithParameter("@userId", userId)
                    .WithParameter("@messageId", messageId)
                ;

            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<QueueMessage>(queryDefinition);
            var currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.FirstOrDefault();
        }
        public async Task<bool> HasUserAnExistingSessionAsync(string userId) => await GetMessageCountAsync(userId) > 0;

        public async Task<bool> MessageExistsAsync(string userId, string messageId)
        {
            var queryDefinition = new QueryDefinition(AddTypeClause($"SELECT VALUE COUNT(1) FROM c WHERE c.userId = @userId AND c.id = @messageId"))
                    .WithParameter("@userId", userId)
                    .WithParameter("@messageId", messageId)
                ;

            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<int>(queryDefinition);
            var currentResults = await queryFeedIterator.ReadNextAsync();

            return currentResults.First() != 0;
        }

        private static string AddOrderBy(string sqlQuery, SearchProperties searchProperties)
        {
            if (searchProperties.Order == null)
            {
                return sqlQuery;
            }

            var sb = new StringBuilder(sqlQuery);

            sb.Append($" {(searchProperties.Sort == null ? string.Empty : "ORDER BY c." + searchProperties.Sort + " " + searchProperties.Order)}");

            return sb.ToString();
        }

        private static string AddSearch(string sqlQuery, SearchProperties searchProperties)
        {
            if (searchProperties.Search == null)
            {
                return sqlQuery;
            }

            var sb = new StringBuilder($"{sqlQuery} AND (");

            foreach (var field in SearchFields)
            {
                sb.Append($" CONTAINS(c.{field}, @{field})");
                sb.Append(" OR ");
            }

            sb.Remove(sb.Length - 3, 3);
            sb.Append(")");

            return sb.ToString();
        }

        private static string AddPaging(string sqlQuery, SearchProperties searchProperties)
        {
            if (searchProperties.Offset == null || searchProperties.Limit == null)
            {
                return sqlQuery;
            }

            var sb = new StringBuilder(sqlQuery);

            sb.Append($" {(searchProperties.Offset == null ? "" : "OFFSET " + searchProperties.Offset)} {(searchProperties.Limit == null ? "" : "LIMIT " + searchProperties.Limit)}");

            return sb.ToString();
        }

        private static string AddTypeClause(string sqlQuery) => sqlQuery + " AND c.type='message'";
        
        public async Task<IEnumerable<UserMessageCount>> GetMessageCountPerUserAsync()
        {            
            var sqlQuery = $"select value messages  from (select c.Queue, c.userId,  count(1) as MessageCount from c where c.type = 'message' group by c.Queue, c.userId ) as messages";

            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<UserMessageCount>(new QueryDefinition(sqlQuery));
            var messages = new List<UserMessageCount>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await queryFeedIterator.ReadNextAsync();
                
                messages.AddRange(currentResults.ToList());
            }

            return messages;
        }
    }
}
