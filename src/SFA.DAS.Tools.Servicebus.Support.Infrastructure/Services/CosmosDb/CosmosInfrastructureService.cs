using System;
using System.Linq;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosInfrastructureService : ICosmosInfrastructureService
    {
        private readonly int _throughput;
        private readonly string _collectionName;
        private readonly string _databaseName;
        private readonly CosmosClient _client;

        public CosmosInfrastructureService(IConfiguration config, CosmosClient client)
        {
            _throughput = config.GetValue<int>("CosmosDb:Throughput");
            _collectionName = config.GetValue<string>("CosmosDb:CollectionName");
            _databaseName = config.GetValue<string>("CosmosDb:DatabaseName");
            _client = client;
        }

        private async Task<Container> CreateContainer(Database database)
        {
            var container = await database.DefineContainer(_collectionName, "/userId")
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
                .CreateIfNotExistsAsync(_throughput)
            ;

            return container.Container;
        }

        private async Task<Database> CreateDatabase()
        {
            return await _client.CreateDatabaseIfNotExistsAsync(_databaseName);
        }

        public async Task<Container> CreateContainer()
        {
            var database = await CreateDatabase();
            return await CreateContainer(database);
        }

        public async Task<FeedIterator<T>> GetItemQueryIterator<T>(QueryDefinition queryDefinition)
        {
            var container = await CreateContainer();
            return container.GetItemQueryIterator<T>(queryDefinition);
        }
    }
}
