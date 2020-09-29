using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching
{
    public interface IBatchSendMessageStrategy
    {
        Task Execute<TIn>(IEnumerable<TIn> messages, int batchSize, Func<IEnumerable<TIn>, Task> sendMessages);
    }
}
