using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching
{
    public class BatchSendMessageStrategy : IBatchSendMessageStrategy
    {
        public async Task Execute(IEnumerable<QueueMessage> messages, Func<IEnumerable<QueueMessage>, Task> sendMessages)
        {
            var po = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 5
            };

            Parallel.ForEach(SplitList(messages.ToList()), po, async batchedMessages =>
            {
                sendMessages(batchedMessages).GetAwaiter().GetResult();
            });
        }

        private IEnumerable<IList<QueueMessage>> SplitList(List<QueueMessage> items)
        {
            // the batch size should be calculated from the message size so as not to exceed the limits (256Mb or 1024KB)
            // we also need to take into account the size of the headers
            // https://diegogiacomelli.com.br/can-i-send-batch-messages-larger-than-256-kb-to-azure-service-bus/

            var maxBatchSize = 256000 - 64000;
            var pageSize = 0L;
            var batches = new List<IList<QueueMessage>>();
            var i = 0;
            var maxBatchCount = 100;

            while (i < items.Count)
            {
                batches.Add(new List<QueueMessage>());

                for (; i < items.Count; i++)
                {
                    var msg = items[i];

                    if ((pageSize + msg.OriginalMessage.GetEstimatedMessageSize() > maxBatchSize) || (batches.Last().Count >= maxBatchCount))
                    {
                        break;
                    }

                    pageSize += msg.OriginalMessage.GetEstimatedMessageSize();
                    batches.Last().Add(msg);
                }

                pageSize = 0;

            }

            return batches;
        }
    }
}
