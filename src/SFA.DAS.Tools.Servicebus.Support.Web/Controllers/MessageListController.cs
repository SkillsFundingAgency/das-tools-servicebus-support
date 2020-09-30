using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.App_Start;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    [ServiceFilter(typeof(KeepUserSessionActiveFilter))]
    public class MessageListController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> _getMessagesQuery;
        private readonly IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> _getMessagesByIdQuery;
        private readonly IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> _getQueueDetailsQuery;
        private readonly ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse> _deleteQueueMessageCommand;
        private readonly IUserSessionService _userSessionService;
        private readonly string _errorQueueRegex;

        public MessageListController(
            IUserService userService,
            IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
            IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> getMessagesByIdQuery,
            IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> getQueueDetailsQuery,
            IMessageService messageService,
            IConfiguration config,
            ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> sendMessagesCommand,
            ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse> deleteQueueMessageCommand,
            IUserSessionService userSessionService)
        {
            _userService = userService;
            _getMessagesQuery = getMessagesQuery;
            _getMessagesByIdQuery = getMessagesByIdQuery;
            _getQueueDetailsQuery = getQueueDetailsQuery;
            _messageService = messageService;
            _userSessionService = userSessionService;
            _deleteQueueMessageCommand = deleteQueueMessageCommand;
            _errorQueueRegex = config["ErrorQueueRegex"];

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

            if (!response.Messages.Any())
            {
                await DeleteUserSession();
                return RedirectToAction("Index", "Servicebus");
            }

            var queueName = GetQueueName(response.Messages);
            HttpContext.Session.SetString("queueName", queueName);            

            return View(new MessageListViewModel()
            {
                Count = response.Count,
                QueueInfo = (await _getQueueDetailsQuery.Handle(new GetQueueDetailsQuery()
                {
                    QueueName = queueName
                })).QueueInfo,
                UserSession = await _userSessionService.GetUserSession()

            });
        }

        public async Task<IActionResult> ReceiveMessages(string queue)
        {            
            await _messageService.GetMessages(queue);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AbortMessages(string data)
        {
            var selectedMessages = JsonConvert.DeserializeObject<SelectedMessages>(data);
            var response = await _getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
            {
                UserId = _userService.GetUserId(),
                Ids = selectedMessages.Ids
            });

            await _messageService.AbortMessages(response.Messages, selectedMessages.Queue);

            return Json(string.Empty);
        }

        public async Task<IActionResult> ReleaseMessages(string queue)
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

            return Json(string.Empty);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessages(string data)
        {
            var selectedMessages = JsonConvert.DeserializeObject<SelectedMessages>(data);
            await _deleteQueueMessageCommand.Handle(new BatchDeleteQueueMessagesCommand()
            {
                Ids = selectedMessages.Ids
            });

            return Json(string.Empty);
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
            return Regex.Replace(errorQueueName, _errorQueueRegex, "");
        }

        private async Task DeleteUserSession()
        {
            await _userSessionService.DeleteUserSession();
        }
    }
}
