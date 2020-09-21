using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageListController : Controller
    {
        private readonly ILogger<MessageListController> _logger;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> _getMessagesQuery;
        private readonly IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> _getMessagesByIdQuery;
        private readonly IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> _getQueueDetailsQuery;
        private readonly ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> _sendMessagesCommand;
        private readonly ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse> _deleteQueueMessageCommand;

        public MessageListController(
            IUserService userService,
            IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
            IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> getMessagesByIdQuery,
            IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> getQueueDetailsQuery,
            IMessageService messageService,
            ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> sendMessagesCommand,
            ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse> deleteQueueMessageCommand,
            ILogger<MessageListController> logger)
        {
            _userService = userService;
            _getMessagesQuery = getMessagesQuery;
            _getMessagesByIdQuery = getMessagesByIdQuery;
            _getQueueDetailsQuery = getQueueDetailsQuery;
            _messageService = messageService;
            _sendMessagesCommand = sendMessagesCommand;
            _deleteQueueMessageCommand = deleteQueueMessageCommand;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _getMessagesQuery.Handle(new GetMessagesQuery() 
            { 
                UserId = _userService.GetUserId(),
                SearchProperties = new SearchProperties
                {
                    Offset = 0,
                    Limit = 1
                }
            });
            
            return View(new MessageListViewModel()
            {
                Count = response.Count,
                QueueInfo = (await _getQueueDetailsQuery.Handle(new GetQueueDetailsQuery()
                {
                    QueueName = GetQueueName(response.Messages)
                })).QueueInfo
            });
        }

        public async Task<IActionResult> ReceiveMessages(string queue)
        {
            await _messageService.ProcessMessages(queue);

            return RedirectToAction("Index");
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AbortMessages(string data)
        {
            var selectedMessages = JsonConvert.DeserializeObject<SelectedMessages>(data);
            var response = await _getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
                {
                    UserId = _userService.GetUserId(),
                    Ids = selectedMessages.Ids
                });

            await _messageService.AbortMessages(response.Messages, selectedMessages.Queue);            

            return RedirectToAction("Index", "Servicebus");
        }

        public async Task<IActionResult> EndSession(string queue)
        {
            var response = await _getMessagesQuery.Handle(new GetMessagesQuery()
            {
                UserId = _userService.GetUserId(),
                SearchProperties = new SearchProperties()
            });

            await _messageService.AbortMessages(response.Messages, queue);

            return RedirectToAction("Index", "Servicebus");
        }

        [HttpPost]
        public async Task<IActionResult> ReplayMessages(string data)
        {
            var selectedMessages = JsonConvert.DeserializeObject<SelectedMessages>(data);
            var response = await _getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
            {
                UserId = _userService.GetUserId(),
                Ids = selectedMessages.Ids
            });

            var processingQueueName = GetProcessingQueueName(selectedMessages.Queue);
            await _messageService.ReplayMessages(response.Messages, processingQueueName);

            return RedirectToAction("Index", "Servicebus");
        }        

        [HttpPost]        
        public async Task<IActionResult> DeleteMessages(string data)            
        {
            var selectedMessages = JsonConvert.DeserializeObject<SelectedMessages>(data);
            await _messageService.DeleteMessages(selectedMessages.Ids);            

            return RedirectToAction("Index", "Servicebus");
        }

        public async Task<IActionResult> Data(string sort, string order, string search, int offset, int limit)
        {
            var response = await _getMessagesQuery.Handle(new GetMessagesQuery()
            {
                UserId = _userService.GetUserId(),
                SearchProperties = new SearchProperties
                {
                    Sort = sort,
                    Order = order,
                    Search = search,
                    Offset = offset,
                    Limit = limit
                }
            });

            var queueMessages = response.Messages.ToList();

            return Json(new
            {
                Total = response.Count,
                TotalNotFiltered = response.UnfilteredCount,
                Rows = queueMessages.Select(msg => new
                {
                    Id = msg.Id,
                    OriginatingEndpoint = msg.OriginatingEndpoint,
                    ProcessingEndpoint = msg.ProcessingEndpoint,
                    Body = msg.Body,
                    Exception = msg.Exception,
                    ExceptionType = msg.ExceptionType
                })
            });
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

        private string GetProcessingQueueName(string errorQueueName)
        {
            string pattern = @"[-,_]error[s]*$";
            return Regex.Replace(errorQueueName, pattern, "");
        }
    }
}
