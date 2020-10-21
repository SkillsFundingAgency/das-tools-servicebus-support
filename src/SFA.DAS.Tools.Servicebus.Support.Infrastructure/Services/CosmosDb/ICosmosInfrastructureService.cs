using System.Linq;
using Microsoft.Azure.Cosmos;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public interface ICosmosInfrastructureService
    {
        Task<Container> CreateContainer();
        Task<FeedIterator<T>> GetItemQueryIterator<T>(QueryDefinition queryDefinition);
    }
}