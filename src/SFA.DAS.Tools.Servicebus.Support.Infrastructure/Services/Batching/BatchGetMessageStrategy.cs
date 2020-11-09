using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching
{
    public class BatchGetMessageStrategy : IBatchGetMessageStrategy
    {
        public async Task<IList<TOut>> Execute<TIn, TOut>(string queueName, long qty, int batchSize, Func<int, Task<IList<TIn>>> getMessages, Func<TIn, Task<TOut>> processMessage)
        {
            var peekedMessages = new ConcurrentBag<TOut>();
            var tasks = new List<Task>();
            var throttler = new SemaphoreSlim(5);
            var maxWaitTime = TimeSpan.FromMinutes(10);
            var runtime = new Stopwatch();
            var isComplete = false;
            var breaker = 0;

            runtime.Start();
            while (!isComplete)
            {
                var remainingQty = qty - peekedMessages.Count;
                var batches = Batches(batchSize, remainingQty);

                await throttler.WaitAsync();

                tasks.AddRange(batches.Select(size => Task.Run(async () =>
                {
                    try
                    {
                        var read = await getMessages(size);

                        Interlocked.Exchange(ref breaker, read == null ? 1 : 0);

                        if (read != null && read.Any())
                        {
                            Parallel.ForEach(read, (msg) =>
                            {
                                var formattedMsg = processMessage(msg).GetAwaiter().GetResult();
                                peekedMessages.Add(formattedMsg);
                            });
                        }
                    }
                    finally
                    {
                        throttler.Release();
                    }
                })));

                await Task.WhenAll(tasks.ToArray());

                isComplete = (peekedMessages.Count >= qty || runtime.Elapsed > maxWaitTime) || breaker == 1;
            }

            return peekedMessages.AsEnumerable().ToList();
        }

        private static IEnumerable<int> Batches(int batchSize, long qty)
        {
            var numOfBatches = qty / batchSize;
            var remainder = qty % batchSize;
            var list = new List<int>();

            for (var i = 0; i < numOfBatches; i++)
            {
                list.Add(batchSize);
            }

            if (remainder > 0)
            {
                list.Add((int) remainder);
            }

            return list;
        }
    }
}
