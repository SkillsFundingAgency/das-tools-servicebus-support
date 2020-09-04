using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageDetailsController : Controller
    {
        private readonly ICosmosMessageService cosmosMessageService;

        public MessageDetailsController(ICosmosMessageService cosmosMessageService)
        {
            this.cosmosMessageService = cosmosMessageService;
        }

        public async Task<IActionResult> Index(string id)
        {
            var errorMessage = await cosmosMessageService.GetErrorMessageAsync("123456", "5e1c1857-e39d-4e1d-baf1-a0c61680c5a2");
            return View(errorMessage);
        }
    }
}
