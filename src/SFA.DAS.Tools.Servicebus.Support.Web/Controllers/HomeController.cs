using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISvcBusService _svcBusService;
        private readonly ICosmosMessageService _cosmosMessageService;

        public HomeController(ILogger<HomeController> logger, ISvcBusService svcBusService, ICosmosMessageService cosmosMessageService)
        {
            _logger = logger;
            _svcBusService = svcBusService;
            _cosmosMessageService = cosmosMessageService;
        }

        public async Task<IActionResult> Index()
        {
            //_svcBusService.GetErrorQueuesAsync();


            var messages = await _svcBusService.PeekMessagesAsync("errors", 100);
            await _cosmosMessageService.BulkCreateAsync(messages);


            var mdbMessages = await _cosmosMessageService.GetErrorMessagesAsync("123456");
            //_svcBusService.SendMessageAsync(messages.ToList()[0]);

            //var messages = await _svcBusService.ReceiveMessagesAsync("errors", 10);


            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
