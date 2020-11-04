using Polly;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb
{
    public interface IServiceBusPolicies
    {
        public IAsyncPolicy ResiliencePolicy { get; }
    }
}