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
        private readonly ICosmosDbContext cosmosMessageService;

        public MessageDetailsController(ICosmosDbContext cosmosMessageService)
        {
            this.cosmosMessageService = cosmosMessageService;
        }

        public async Task<IActionResult> Index(string id)
        {
            var errorMessage = await cosmosMessageService.GetQueueMessageAsync(UserService.GetUserId(), id);
            return View(errorMessage);
        }
    }
}
