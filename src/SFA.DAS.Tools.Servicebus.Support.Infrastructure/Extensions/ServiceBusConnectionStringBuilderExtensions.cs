using Microsoft.Azure.ServiceBus;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class ServiceBusConnectionStringBuilderExtensions
    {
        public static bool HasSasKey(this ServiceBusConnectionStringBuilder connectionBuilder)
        {
            return connectionBuilder.SasKey?.Length > 0;
        }
    }
}
