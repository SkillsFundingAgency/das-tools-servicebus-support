using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.App_Start;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    [ServiceFilter(typeof(KeepUserSessionActiveFilter))]
    public class MessageListController : Controller
    {
        private readonly IUserService _userService;
        private readonly IMessageService _messageService;
        private readonly IRetrieveMessagesService _retrieveMessagesService;
        private readonly IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> _getMessagesQuery;
        private readonly IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> _getMessagesByIdQuery;
        private readonly IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> _getQueueDetailsQuery;
        private readonly IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse> _getQueueMessageCountQuery;
        private readonly ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse>
            _deleteQueueMessageCommand;

        private readonly IUserSessionService _userSessionService;
        private readonly ServiceBusErrorManagementSettings _settings;

        public MessageListController(
            IUserService userService,
            IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
            IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> getMessagesByIdQuery,
            IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> getQueueDetailsQuery,
            IMessageService messageService,
            IUserSessionService userSessionService,
            IOptions<ServiceBusErrorManagementSettings> settings,
            ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse> sendMessagesCommand,
            ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse> deleteQueueMessageCommand,
            IRetrieveMessagesService retrieveMessagesService,
            IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse> getQueueMessageCountQuery)
        {
            _userService = userService;
            _getMessagesQuery = getMessagesQuery;
            _getMessagesByIdQuery = getMessagesByIdQuery;
            _getQueueDetailsQuery = getQueueDetailsQuery;
            _messageService = messageService;
            _userSessionService = userSessionService;
            _settings = settings.Value;
            _deleteQueueMessageCommand = deleteQueueMessageCommand;
            _retrieveMessagesService = retrieveMessagesService;
            _getQueueMessageCountQuery = getQueueMessageCountQuery;
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

            var queueName = response.Messages.GetQueueName();

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

        [HttpPost]
        public async Task<IActionResult> ReceiveMessagesFromQueue(ReceiveMessagesModel model)
        {
            var count = (await _getQueueMessageCountQuery.Handle(new GetQueueMessageCountQuery()
            {
                QueueName = model.QueueName
            })).Count;

            await _retrieveMessagesService.GetMessages(model.QueueName, count, model.GetQuantity);

            // Will always update session on receiving messages but removes reliance on HttpContext Session
            await _userSessionService.UpsertUserSession(model.QueueName);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ReleaseSelectedMessages(ReleaseSelectedMessages model)
        {
            var response = await _getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
            {
                UserId = _userService.GetUserId(),
                Ids = model.Ids?.Split(",")
            });

            await _messageService.AbortMessages(response.Messages, model.QueueName);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ReleaseAllMessages(string queue)
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
        public async Task<IActionResult> ReplayMessages(ReplayMessagesModel model)
        {
            var response = await _getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
            {
                UserId = _userService.GetUserId(),
                Ids = model.Ids?.Split(",")
            });

            var processingQueueName = model.QueueName.GetProcessingQueueName(_settings.ErrorQueueRegex);
            await _messageService.ReplayMessages(response.Messages, processingQueueName);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessages(DeleteMessagesModel model)
        {
            await _deleteQueueMessageCommand.Handle(new BatchDeleteQueueMessagesCommand()
            {
                Ids = model.Ids?.Split(",")
            });

            return RedirectToAction("Index");
        }

        private async Task DeleteUserSession()
        {
            await _userSessionService.DeleteUserSession();
            HttpContext.Session.Set<DateTime?>("sessionActiveUntil", null);
        }
    }
}
