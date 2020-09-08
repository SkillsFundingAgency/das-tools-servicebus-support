using Microsoft.Azure.ServiceBus.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Core.Models
{
    public class ReceiveMessagesResponse
    {
        public IEnumerable<QueueMessage> Messages { get; set; }
        public MessageReceiver MessageReceiver { get; set; }


    }
}
