using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class ReceiveMessagesModel
    {
        public string QueueName { get; set; }
        public int GetQuantity { get; set; }
    }
}
