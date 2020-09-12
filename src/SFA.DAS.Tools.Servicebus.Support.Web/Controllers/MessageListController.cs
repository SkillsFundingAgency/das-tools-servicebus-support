using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using SFA.DAS.Tools.Servicebus.Support.Core.Models;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

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
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to receive messages", ex);
                    ts.Dispose();
                }
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> EndSession()
        {
            var allMessages = await _cosmosDbContext.GetQueueMessagesAsync(UserService.GetUserId(), new SearchProperties());
            var messageIds = allMessages.Select(x => x.Id).ToList();

            await AbortMessages(messageIds);

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> AbortMessages(IEnumerable<string> messageIds)
        {
            foreach (var batchedIds in SplitList<string>(messageIds.ToList()))
            {
                using (var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    try
                    {
                        var messages = await _cosmosDbContext.GetQueueMessagesByIdAsync(UserService.GetUserId(), batchedIds);
                        if (messages.Count() > 0)
                        {
                            var errorQueueName = GetQueueName(messages);

                            await _svcBusService.SendMessagesToErrorQueueAsync(messages, errorQueueName);
                            var ids = messages.Select(x => x.Id).ToList();
                            await _cosmosDbContext.DeleteQueueMessagesAsync(ids);
                        }
                        ts.Complete();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Failed to abort messages", ex);
                        ts.Dispose();
                    }
                }
            }

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> DeleteMessages(IEnumerable<string> messageIds)
        {
            //todo test only 
            var allMessages = await _cosmosDbContext.GetQueueMessagesAsync(UserService.GetUserId(), new SearchProperties());
            messageIds = allMessages.Select(x => x.Id).ToList().Take(1);

            foreach (var batchedIds in SplitList<string>(messageIds.ToList()))
            {
                try
                {                    
                    await _cosmosDbContext.DeleteQueueMessagesAsync(batchedIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Failed to delete messages", ex);

                }
            }

            return RedirectToAction("Index", "Home");
        }

        public static IEnumerable<List<T>> SplitList<T>(List<T> items, int nSize = 25)
        {
            for (int i = 0; i < items.Count(); i += nSize)
            {
                yield return items.GetRange(i, Math.Min(nSize, items.Count - i));
            }
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
