using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using Polly.Timeout;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosMessageDbContext : ICosmosMessageDbContext
    {
        private readonly ILogger<CosmosMessageDbContext> _logger;
        private readonly ICosmosInfrastructureService _cosmosInfrastructure;
        private readonly IUserService _userService;
        private readonly ICosmosDbPolicies _policies;
        private const string MessageType = "message";

        public CosmosMessageDbContext(IUserService userService, ILogger<CosmosMessageDbContext> logger, ICosmosInfrastructureService cosmosInfrastructure, ICosmosDbPolicies policies)
        {
            _userService = userService;
            _logger = logger;
            _cosmosInfrastructure = cosmosInfrastructure;
            _policies = policies;
        }

        public async Task CreateQueueMessageAsync(QueueMessage msg)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            await _policies.ResiliencePolicy.ExecuteAsync(() => container.CreateItemAsync(msg));
        }

        public async Task BulkCreateQueueMessagesAsync(IEnumerable<QueueMessage> messages)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var batchResponse = default(TransactionalBatchResponse);

            await _policies.BulkBatchPolicy.ExecuteAsync(async () =>
            {
                var batch = container.CreateTransactionalBatch(new PartitionKey(_userService.GetUserId()));

                foreach (var msg in messages)
                {
                    batch.CreateItem(msg);
                }

                batchResponse = await batch.ExecuteAsync();
            });

            if (!batchResponse.IsSuccessStatusCode)
            {
                _logger.LogError("Cosmos batch creation failed, {0}", batchResponse.StatusCode);
                throw new CosmosBatchInsertException("Cosmos batch creation failed", batchResponse.StatusCode);
            }
        }

        public async Task DeleteQueueMessagesAsync(IEnumerable<string> ids)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var batchResponse = default(TransactionalBatchResponse);

            await _policies.BulkBatchPolicy.ExecuteAsync(async () =>
            {
                var batch = container.CreateTransactionalBatch(new PartitionKey(_userService.GetUserId()));

                foreach (var id in ids)
                {
                    batch.DeleteItem(id);
                }

                batchResponse = await batch.ExecuteAsync();
            });

            if (!batchResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"Cosmos batch deletion failed: {batchResponse.ErrorMessage}");
                throw new CosmosDeleteException("Cosmos batch deletion failed", batchResponse.StatusCode);
            }
        }

        public async Task<IEnumerable<QueueMessage>> GetQueueMessagesAsync(string userId, SearchProperties searchProperties)
        {
            searchProperties ??= new SearchProperties();

            var container = await _cosmosInfrastructure.CreateContainer();
            var queryDefinition = container.GetItemLinqQueryable<QueueMessage>()
                .Where(m => m.UserId == userId && m.Type == MessageType)
            ;

            queryDefinition = AddSearchTermToQueryDefinition(searchProperties, queryDefinition);
            queryDefinition = AddSortingToQueryDefinition(searchProperties, queryDefinition);
            queryDefinition = AddPagingToQueryDefinition(searchProperties, queryDefinition);

            var queryFeedIterator = queryDefinition.ToFeedIterator();
            var messages = new List<QueueMessage>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await _policies.ResiliencePolicy.ExecuteAsync(() => queryFeedIterator.ReadNextAsync());

                messages.AddRange(currentResults.ToList());
            }

            return messages;
        }

        public async Task<IEnumerable<QueueMessage>> GetQueueMessagesByIdAsync(string userId, IEnumerable<string> ids)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var queryFeedIterator = container.GetItemLinqQueryable<QueueMessage>()
                    .Where(m => m.UserId == userId && ids.Contains(m.Id))
                    .ToFeedIterator()
                ;

            var messages = new List<QueueMessage>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await _policies.ResiliencePolicy.ExecuteAsync(() => queryFeedIterator.ReadNextAsync());

                var newMessages = currentResults.ToList();
                messages.AddRange(newMessages);
            }

            return messages;
        }

        public async Task<int> GetMessageCountAsync(string userId, SearchProperties searchProperties = null)
        {
            searchProperties ??= new SearchProperties();

            var container = await _cosmosInfrastructure.CreateContainer();
            var queryDefinition = container.GetItemLinqQueryable<QueueMessage>()
                    .Where(m => m.UserId == userId && m.Type == MessageType)
                ;

            queryDefinition = AddSearchTermToQueryDefinition(searchProperties, queryDefinition);
            var result = await _policies.ResiliencePolicy.ExecuteAsync(() => queryDefinition.CountAsync());

            return result.Resource;
        }

        public async Task<QueueMessage> GetQueueMessageAsync(string userId, string messageId)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var queryFeedIterator = container.GetItemLinqQueryable<QueueMessage>()
                    .Where(c => c.UserId == userId && c.Id == messageId && c.Type == MessageType)
                    .ToFeedIterator()
                ;

            var currentResults = await _policies.ResiliencePolicy.ExecuteAsync(() => queryFeedIterator.ReadNextAsync());

            return currentResults.FirstOrDefault();
        }
        public async Task<bool> HasUserAnExistingSessionAsync(string userId) => await GetMessageCountAsync(userId) > 0;

        public async Task<bool> MessageExistsAsync(string userId, string messageId)
        {
            var container = await _cosmosInfrastructure.CreateContainer();
            var queryDefinition = container.GetItemLinqQueryable<QueueMessage>()
                    .Where(m => m.UserId == userId && m.Id == messageId && m.Type == MessageType)
                ;

            var result = await _policies.ResiliencePolicy.ExecuteAsync(() => queryDefinition.CountAsync());

            return result.Resource != 0;
        }

        private static IQueryable<QueueMessage> AddSearchTermToQueryDefinition(SearchProperties searchProperties, IQueryable<QueueMessage> queryDefinition)
        {
            if (searchProperties.Search != null)
            {
                queryDefinition = queryDefinition.Where(m => m.Body.Contains(searchProperties.Search)
                                                             || m.ProcessingEndpoint.Contains(searchProperties.Search)
                                                             || m.OriginatingEndpoint.Contains(searchProperties.Search)
                                                             || m.Exception.Contains(searchProperties.Search)
                                                             || m.ExceptionType.Contains(searchProperties.Search));
            }

            return queryDefinition;
        }

        private static IQueryable<QueueMessage> AddSortingToQueryDefinition(SearchProperties searchProperties, IQueryable<QueueMessage> queryDefinition)
        {
            if (!string.IsNullOrEmpty(searchProperties.Order))
            {
                queryDefinition = queryDefinition.OrderBy($"{searchProperties.Sort} {searchProperties.Order}");
            }

            return queryDefinition;
        }

        private static IQueryable<QueueMessage> AddPagingToQueryDefinition(SearchProperties searchProperties, IQueryable<QueueMessage> queryDefinition)
        {
            if (searchProperties.Offset.HasValue && searchProperties.Limit.HasValue)
            {
                queryDefinition = queryDefinition.Skip(searchProperties.Offset.Value).Take(searchProperties.Limit.Value);
            }

            return queryDefinition;
        }

        public async Task<IEnumerable<UserMessageCount>> GetMessageCountPerUserAsync()
        {
            var sqlQuery = $"select value messages  from (select c.Queue, c.userId,  count(1) as MessageCount from c where c.type = 'message' group by c.Queue, c.userId ) as messages";

            var queryFeedIterator = await _cosmosInfrastructure.GetItemQueryIterator<UserMessageCount>(new QueryDefinition(sqlQuery));
            var messages = new List<UserMessageCount>();

            while (queryFeedIterator.HasMoreResults)
            {
                var currentResults = await _policies.ResiliencePolicy.ExecuteAsync(() => queryFeedIterator.ReadNextAsync());

                messages.AddRange(currentResults.ToList());
            }

            return messages;
        }
    }
}
