using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching
{
    public class BatchGetMessageStrategy : IBatchGetMessageStrategy
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
            }

            return peekedMessages;
        }
    }
}
