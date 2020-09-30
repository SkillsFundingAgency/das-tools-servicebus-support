using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages
{
    public class DeleteQueueMessagesCommand
    {
        public IEnumerable<string> Ids { get; set; }
    }
}
