using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosInfrastructureService : ICosmosInfrastructureService
    {
        private readonly int _throughput;
        private readonly string _collectionName;

        public CosmosInfrastructureService(IConfiguration config)
        {
            _throughput = config.GetValue<int>("CosmosDb:Throughput");
            _collectionName = config.GetValue<string>("CosmosDb:CollectionName");            
        }

        public async Task<Container> CreateContainer(Database database)
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
    }
}
