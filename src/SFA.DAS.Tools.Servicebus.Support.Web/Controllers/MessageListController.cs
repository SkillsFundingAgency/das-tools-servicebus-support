using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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
        private readonly ILogger<MessageListController> _logger;

        public MessageListController(ISvcBusService svcBusService, ICosmosDbContext cosmosDbContext, ILogger<MessageListController> logger)
        {
            _svcBusService = svcBusService;
            _cosmosDbContext = cosmosDbContext;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var messages = await _cosmosDbContext.GetQueueMessagesAsync(UserService.GetUserId(), new SearchProperties
            {
                Offset = 0,
                Limit = 1
            });
            var cnt = await _cosmosDbContext.GetUserMessageCountAsync(UserService.GetUserId());
            var queueName = GetQueueName(messages);

            var vm = new MessageListViewModel()
            {
                Count = cnt,
                QueueInfo = await _svcBusService.GetQueueDetailsAsync(queueName)
            };

            return View(vm);
        }


        public async Task<IActionResult> ReceiveMessages(string queue)
        {
            using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                ReceiveMessagesResponse response = null;

                try
                {
                    response = await _svcBusService.ReceiveMessagesAsync(queue, 50);//todo custom qty 
                    await _cosmosDbContext.BulkCreateQueueMessagesAsync(response.Messages);
                    
                    ts.Complete();
                }catch(Exception ex)
                {
                    _logger.LogError("Failed to receive messages", ex);
                    ts.Dispose();
                }                               
            }                        

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> AbortMessages()
        {
            var messages = await _cosmosDbContext.GetQueueMessagesAsync(UserService.GetUserId(), new SearchProperties());

            foreach (var msg in messages)
            {
                await _svcBusService.SendMessageToErrorQueueAsync(msg);
                await _cosmosDbContext.DeleteQueueMessageAsync(msg);
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Data(string sort, string order, string search, int offset, int limit)
        {
            var messages = await _cosmosDbContext.GetQueueMessagesAsync(UserService.GetUserId(), new SearchProperties
                {
                    Sort = sort, 
                    Order = order, 
                    Search = search, 
                    Offset = offset, 
                    Limit = limit
                });
            var cnt = await _cosmosDbContext.GetUserMessageCountAsync(UserService.GetUserId());
            var queueMessages = messages.ToList();

            DefaultContractResolver contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new DefaultNamingStrategy()
            };

            return Json(new
            {
                Total = cnt,
                TotalNotFiltered = cnt,
                Rows = queueMessages.Select(msg => new
                {
                    Id = msg.Id,
                    OriginatingEndpoint = msg.OriginatingEndpoint,
                    ProcessingEndpoint = msg.ProcessingEndpoint,
                    Body = msg.Body,
                    Exception = msg.Exception,
                    ExceptionType = msg.ExceptionType
                })
            }/*, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = null
            }*/);
        }

        private string GetQueueName(IEnumerable<QueueMessage> messages)
        {
            var name = "";
            if (messages?.Count() > 0)
            {
                name = messages.First().Queue;
            }

            return name;
        }
    }
}
