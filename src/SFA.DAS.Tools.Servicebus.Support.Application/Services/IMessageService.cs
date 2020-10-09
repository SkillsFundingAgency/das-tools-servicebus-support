using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public interface IMessageService
    {
        Task AbortMessages(IEnumerable<QueueMessage> messages, string queue);
        Task ReplayMessages(IEnumerable<QueueMessage> messages, string queue);
    }
}
