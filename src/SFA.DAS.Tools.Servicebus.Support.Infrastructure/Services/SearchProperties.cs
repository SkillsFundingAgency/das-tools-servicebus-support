using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class SearchProperties
    {
        public string Sort { get; set; }
        public string Order { get; set; }
        public string Search { get; set; }
        public int? Offset { get; set; }
        public int? Limit { get; set; }
    }
}
