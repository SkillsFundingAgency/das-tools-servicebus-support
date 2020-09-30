using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages
{
    public class BatchDeleteQueueMessagesCommand
    {
        public IEnumerable<string> Ids { get; set; }
    }
}
