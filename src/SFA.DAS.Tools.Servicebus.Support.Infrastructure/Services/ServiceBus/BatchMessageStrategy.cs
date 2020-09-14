using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class BatchMessageStrategy : IBatchMessageStrategy
    {
        public async Task<IList<TOut>> Execute<TIn, TOut>(string queueName, long qty, int batchSize, Func<int, Task<IList<TIn>>> getMessages, Func<TIn, Task<TOut>> processMessage)
        {
            var totalMessages = 0;
            var peekedMessages = new List<TOut>();

            while (totalMessages < qty)
            {
                var read = await getMessages(batchSize);

                if (read == null || read.Count <= 0)
                {
                    break;
                }
                
                totalMessages += read.Count;

                foreach (var msg in read)
                {
                    var formattedMsg = await processMessage(msg);
                    peekedMessages.Add(formattedMsg);
                }

                if (read.Count < batchSize)
                {
                    break;
                }
            }

            return peekedMessages;
        }
    }
}
