using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public class CosmosInfrastructureService : ICosmosInfrastructureService
    {
        private readonly int _throughput;

        public CosmosInfrastructureService(IConfiguration config)
        {
            _throughput = config.GetValue<int>("CosmosDb:Throughput");
        }

        public async Task<Container> CreateContainer(Database database)
        {
            var container = await database.DefineContainer("Session", "/userId")
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
