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
        private readonly ICosmosMessageService _cosmosMessageService;


        public HomeController(ILogger<HomeController> logger, ISvcBusService svcBusService, ICosmosMessageService cosmosMessageService)
        {
            _logger = logger;
            _svcBusService = svcBusService;
            _cosmosMessageService = cosmosMessageService;

        }

        public async Task<IActionResult> Index()
        {
            var searchVM = new SearchViewModel
            {
                Queues = new SelectList(await _svcBusService.GetErrorQueuesAsync()),
                ErrorMessages = new List<ErrorMessage>()
            };
            //HttpContext.Session.Set<SearchViewModel>("searchVM", searchVM);

            //var messages = await _svcBusService.PeekMessagesAsync("errors", 100);
            //await _cosmosMessageService.BulkCreateAsync(messages);


            //var mdbMessages = await _cosmosMessageService.GetErrorMessagesAsync("123456");
            //_svcBusService.SendMessageAsync(messages.ToList()[0]);

            //var messages = await _svcBusService.ReceiveMessagesAsync("errors", 10);


            return View(searchVM);
        }


        public async Task<IActionResult> Peek(string selectedQueue)
        {
            SearchViewModel searchVM = null;// HttpContext.Session.Get<SearchViewModel>("searchVM");

            if (searchVM == null)
                searchVM = new SearchViewModel();

            searchVM.ErrorMessages = await _svcBusService.PeekMessagesAsync(selectedQueue, 100);

            return View("Index", searchVM);
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
