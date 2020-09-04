using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageListController : Controller
    {
        private readonly ISvcBusService _svcBusService;

        public MessageListController(ISvcBusService svcBusService)
        {
            _svcBusService = svcBusService;
        }

        public async Task<IActionResult> Index(string selectedQueue)
        {           
            var queueMessages = await _svcBusService.PeekMessagesAsync(selectedQueue, 100);

            return View(queueMessages);
        }
    }
}
