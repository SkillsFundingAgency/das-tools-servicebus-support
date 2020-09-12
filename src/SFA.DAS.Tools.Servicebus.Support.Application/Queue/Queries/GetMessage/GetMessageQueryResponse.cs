using System;
using System.Collections.Generic;
using System.Text;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage
{
    public class GetMessageQueryResponse
    {
        public QueueMessage Message { get; set; }
    }
}
