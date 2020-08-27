using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Core.Models
{
    public class ErrorMessage
    {
        public Guid id { get; set; }
        public string userId { get; set; }
        public Message OriginalMessage { get; set; }
        public string Queue { get; set; }
        public bool IsReadOnly { get; set; }
    }
}
