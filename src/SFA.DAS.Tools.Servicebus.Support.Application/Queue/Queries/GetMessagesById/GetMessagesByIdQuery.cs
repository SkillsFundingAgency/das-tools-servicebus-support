using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById
{
    public class GetMessagesByIdQuery
    {
        public string UserId { get; set; }
        public IEnumerable<string> Ids { get; set; }
    }
}
