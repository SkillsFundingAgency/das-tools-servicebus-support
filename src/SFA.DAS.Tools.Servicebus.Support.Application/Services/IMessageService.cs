using Microsoft.AspNetCore.Routing;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public interface IMessageService
    {
        Task ProcessMessages(string queue, Transactional transaction = Transactional.Yes);
        Task AbortMessages(IEnumerable<QueueMessage> messages, string queue);
        Task ReplayMessages(IEnumerable<QueueMessage> messages, string queue);
        Task DeleteMessages(IEnumerable<string> ids);
    }
}
