using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using Microsoft.Azure.ServiceBus.Primitives;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public class MessageReceiverFactory : IMessageReceiverFactory
    {
        private readonly ServiceBusConnectionStringBuilder _sbConnectionStringBuilder;
        private readonly ITokenProvider _tokenProvider;

        public MessageReceiverFactory(
            ServiceBusConnectionStringBuilder sbConnectionStringBuilder,
            ITokenProvider tokenProvider)
        {
            _sbConnectionStringBuilder = sbConnectionStringBuilder;
            _tokenProvider = tokenProvider;
        }

        public IMessageReceiver Create(string queueName)
        {
            return _sbConnectionStringBuilder.HasSasKey()
                ? new MessageReceiver(new ServiceBusConnection(_sbConnectionStringBuilder), queueName)
                : new MessageReceiver(_sbConnectionStringBuilder.Endpoint, queueName, _tokenProvider);
        }
    }
}
