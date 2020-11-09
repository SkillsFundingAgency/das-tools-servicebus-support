using Polly;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public interface ICosmosDbPolicies
    {
        public IAsyncPolicy ResiliencePolicy { get; }
        public IAsyncPolicy BulkBatchPolicy { get; }
    }
}