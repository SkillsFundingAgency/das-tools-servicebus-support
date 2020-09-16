using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessageToErrorQueue;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageListController : Controller
    {
        private readonly ILogger<MessageListController> _logger;
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> _getMessagesQuery;
        private readonly IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> _getQueueDetailsQuery;
        private readonly ICommandHandler<SendMessageToErrorQueueCommand, SendMessageToErrorQueueCommandResponse> _sendMessageToErrorQueueCommand;
        private readonly ICommandHandler<DeleteQueueMessageCommand, DeleteQueueMessageCommandResponse> _deleteQueueMessageCommand;

        public MessageListController(
            IUserService userService,
            IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
            IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> getQueueDetailsQuery,
            IMessageService messageService,
            ICommandHandler<SendMessageToErrorQueueCommand, SendMessageToErrorQueueCommandResponse> sendMessageToErrorQueueCommand,
            ICommandHandler<DeleteQueueMessageCommand, DeleteQueueMessageCommandResponse> deleteQueueMessageCommand,
            ILogger<MessageListController> logger)
        {
            _userService = userService;
            _getMessagesQuery = getMessagesQuery;
            _getQueueDetailsQuery = getQueueDetailsQuery;
            _messageService = messageService;
            _sendMessageToErrorQueueCommand = sendMessageToErrorQueueCommand;
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

        public async Task<IActionResult> AbortMessages()
        {
            var response = await _getMessagesQuery.Handle(new GetMessagesQuery()
                {
                    UserId = _userService.GetUserId(),
                    SearchProperties = new SearchProperties()

                });

            foreach (var msg in response.Messages)
            {
                await _sendMessageToErrorQueueCommand.Handle(new SendMessageToErrorQueueCommand()
                    {
                        Message = msg
                    });

                await _deleteQueueMessageCommand.Handle(new DeleteQueueMessageCommand()
                    {
                        Message = msg
                    });
            }

            return RedirectToAction("Index", "Home");
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
    }
}
