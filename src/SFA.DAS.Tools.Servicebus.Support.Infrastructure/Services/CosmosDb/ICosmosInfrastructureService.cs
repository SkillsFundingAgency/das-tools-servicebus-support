using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public interface ICosmosInfrastructureService
    {
        Task<Container> CreateContainer(Database database);
    }
}