using Microsoft.AspNetCore.Mvc.Rendering;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models
{
    public class SearchViewModel
    {
        //public IEnumerable<string> Queues { get; set; }
        public SelectList Queues { get; set; }
        public string SelectedQueue { get; set; }
        public IEnumerable<ErrorMessage> ErrorMessages { get; set; }
    }
}
