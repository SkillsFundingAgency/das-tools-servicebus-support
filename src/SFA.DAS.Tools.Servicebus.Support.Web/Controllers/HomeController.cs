using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISvcBusService _svcBusService;
        private readonly ICosmosDbContext _cosmosDbContext;


        public HomeController(ILogger<HomeController> logger, ISvcBusService svcBusService, ICosmosDbContext cosmosDbContext)
        {
            _logger = logger;
            _svcBusService = svcBusService;
            _cosmosDbContext = cosmosDbContext;

        }

        public async Task<IActionResult> Index()
        {

            var count = await _cosmosDbContext.GetUserMessageCountAsync(UserService.GetUserId());

            if (count > 0)
            {
                //user has exisitng session
                return RedirectToAction(actionName: "Index", controllerName: "MessageList");
            }
            else
            {
                var searchVM = new QueueViewModel
                {
                    Queues = await _svcBusService.GetErrorMessageQueuesAsync(),
                };

                return View(searchVM);
            }


            //HttpContext.Session.Set<SearchViewModel>("searchVM", searchVM);

            //var messages = await _svcBusService.PeekMessagesAsync("sfa.das.notifications.messagehandlers-errors", 250);
            //await _cosmosDbContext.BulkCreateQueueMessagesAsync(messages);


            //var mdbMessages = await _cosmosDbContext.GetQueueMessagesAsync("123456");
            //foreach (var msg in mdbMessages)
            //{
            //    await _svcBusService.SendMessageToErrorQueueAsync(msg);
            //}


            //var messages = await _svcBusService.ReceiveMessagesAsync("errors", 10);

            //return View();

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
