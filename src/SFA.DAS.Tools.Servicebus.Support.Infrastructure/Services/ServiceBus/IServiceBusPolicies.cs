using Polly;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus
{
    public interface IServiceBusPolicies
    {
        public IAsyncPolicy ResiliencePolicy { get; }
    }
}