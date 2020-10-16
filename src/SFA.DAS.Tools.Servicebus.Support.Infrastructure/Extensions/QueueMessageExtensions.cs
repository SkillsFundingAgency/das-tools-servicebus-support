using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class QueueMessageExtensions
    {
        public static string GetQueueName(this IEnumerable<QueueMessage> messages)
        {
            if (messages == null || !messages.Any())
            {
                return string.Empty;
            }

            return messages.First().Queue;
        }
    }
}
