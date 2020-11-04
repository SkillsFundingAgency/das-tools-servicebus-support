using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private readonly ITokenProvider _tokenProvider;

        public MessageReceiverFactory(ServiceBusErrorManagementSettings serviceBusSettings)
        {
            _sbConnectionStringBuilder = new ServiceBusConnectionStringBuilder(serviceBusSettings.ServiceBusConnectionString);
            _tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();
                    }

        public IMessageReceiver Create(string queueName)
        {
            return _sbConnectionStringBuilder.HasSasKey()
                ? new MessageReceiver(new ServiceBusConnection(_sbConnectionStringBuilder), queueName)
                : new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);
        }
    }
}
