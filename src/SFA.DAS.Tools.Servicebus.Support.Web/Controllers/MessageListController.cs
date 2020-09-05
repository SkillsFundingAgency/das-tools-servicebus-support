using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageListController : Controller
    {
        private readonly ISvcBusService _svcBusService;
        private readonly ICosmosDbContext _cosmosDbContext;

        public MessageListController(ISvcBusService svcBusService, ICosmosDbContext cosmosDbContext)
        {
            _svcBusService = svcBusService;
            _cosmosDbContext = cosmosDbContext;
        }

        public async Task<IActionResult> Index()
        {
            var messages = await _cosmosDbContext.GetQueueMessagesAsync(UserService.GetUserId());

            var queueName = getQueueName(messages);

            var vm = new MessageListViewModel()
            {
                Messages = messages,
                QueueInfo = await _svcBusService.GetQueueDetailsAsync(queueName)
            };            

            return View(vm);
        }


        public async Task<IActionResult> ReceiveMessages(string queue)
        {
            var messages = await _svcBusService.ReceiveMessagesAsync(queue, 1);
            await _cosmosDbContext.BulkCreateQueueMessagesAsync(messages);
            
            return RedirectToAction("Index");
        }

        private string getQueueName(IEnumerable<QueueMessage> messages)
        {
            var name = "";
            if ( messages?.Count() > 0 )
            {
                name = messages.First().Queue;
            }

            return name;
        }
    }
}
