using Microsoft.Azure.ServiceBus.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class MessageReceiverExtensions
    {
        public static async Task RenewLockAsync(this IMessageReceiver messageReceiver, IEnumerable<string> lockTokens)
        {
            foreach (var lockToken in lockTokens)
            {
                await messageReceiver.RenewLockAsync(lockToken);
            }
        }
    }
}
