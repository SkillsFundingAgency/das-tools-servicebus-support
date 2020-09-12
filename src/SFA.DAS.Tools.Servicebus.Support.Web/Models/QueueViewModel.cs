using Microsoft.AspNetCore.Mvc.Rendering;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class QueueViewModel
    {        
        public IEnumerable<QueueInfo> Queues { get; set; }
        public string SelectedQueue { get; set; }        
    }
}
