using System;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosInfrastructureService : ICosmosInfrastructureService
    {
        private readonly int _throughput;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly CosmosClient _client;
        private readonly ICosmosDbPolicies _policies;

        public CosmosInfrastructureService(CosmosDbSettings cosmosDbSettings, CosmosClient client, ICosmosDbPolicies policies)
        {
            _throughput = cosmosDbSettings.Throughput;
            _collectionName = cosmosDbSettings.CollectionName;
            _databaseName = cosmosDbSettings.DatabaseName;
            _client = client;
            _policies = policies;
        }

        private async Task<Container> CreateContainer(Database database)
        {
            var container = await _policies.ResiliencePolicy.ExecuteAsync(() =>
               database.DefineContainer(_collectionName, "/userId")
               .WithIndexingPolicy()
               .WithIncludedPaths()
                   .Path("/originatingEndpoint/?")
                   .Path("/processingEndpoint/?")
                   .Path("/body/?")
                   .Path("/exception/?")
                   .Path("/exceptionType/?")
                   .Path("/type/?")
                   .Attach()
               .WithExcludedPaths()
                   .Path("/*")
                   .Attach()
               .Attach()
               .CreateIfNotExistsAsync(_throughput));

            return container.Container;
        }

        private async Task<Database> CreateDatabase()
        {
            return await _policies.ResiliencePolicy.ExecuteAsync(() => _client.CreateDatabaseIfNotExistsAsync(_databaseName));
        }

        public async Task<Container> CreateContainer()
        {
            var database = await CreateDatabase();
            return await CreateContainer(database);
        }

        public async Task<FeedIterator<T>> GetItemQueryIterator<T>(QueryDefinition queryDefinition)
        {
            var container = await CreateContainer();
            return await _policies.ResiliencePolicy.ExecuteAsync(() => Task.FromResult(container.GetItemQueryIterator<T>(queryDefinition)));
        }
    }
}
