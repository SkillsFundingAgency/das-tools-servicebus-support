using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching
{
    public class BatchSendMessageStrategy : IBatchSendMessageStrategy
    {
        public async Task Execute<TIn>(IEnumerable<TIn> messages, int batchSize, Func<IEnumerable<TIn>, Task> sendMessages)
        {
            foreach (var batchedMessages in SplitList(messages.ToList(), batchSize))
            {
                await sendMessages(batchedMessages);
            }
        }

        private IEnumerable<IList<TIn>> SplitList<TIn>(List<TIn> items, int size = 25)
        {
            for (var i = 0; i < items.Count; i += size)
            {
                yield return items.GetRange(i, Math.Min(size, items.Count - i));
            }
        }
    }
}
