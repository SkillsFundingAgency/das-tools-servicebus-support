using Microsoft.Azure.ServiceBus.Core;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public interface IMessageReceiverFactory
    {
        IMessageReceiver Create(string queueName);
    }
}
