using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISvcBusService _svcBusService;

        public HomeController(ILogger<HomeController> logger, ISvcBusService svcBusService)
        {
            _logger = logger;
            _svcBusService = svcBusService;
        }

        public IActionResult Index()
        {
            _svcBusService.GetErrorQueuesAsync();

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
