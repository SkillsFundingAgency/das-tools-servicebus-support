using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class QueueInformationModel
    {
        public long Total { get; set; }
        public IEnumerable<QueueCountInfo> Rows { get; set; }

        public class QueueCountInfo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public long MessageCount { get; set; }
            public string MessageCountInvestigation { get; set; }
        }
    }
}
