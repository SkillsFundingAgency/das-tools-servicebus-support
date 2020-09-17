using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessageToErrorQueue;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class ServicebusController : Controller
    {
        private readonly ILogger<ServicebusController> _logger;
        private readonly IUserService _userService;
        private readonly IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> _getUserSessionQuery;
        private readonly IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse> _getQueuesQuery;
        private readonly IQueryHandler<PeekQueueMessagesQuery, PeekQueueMessagesQueryResponse> _peekQueueMessagesQuery;
        private readonly IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> _getMessagesQuery;
        private readonly ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse> _bulkCreateMessagesCommand;
        private readonly ICommandHandler<SendMessageToErrorQueueCommand, SendMessageToErrorQueueCommandResponse> _sendMessageToErrorQueueCommand;

        public ServicebusController(ILogger<ServicebusController> logger,
            IUserService userService,
            IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> getUserSessionQuery,
            IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse> getQueuesQuery,
            IQueryHandler<PeekQueueMessagesQuery, PeekQueueMessagesQueryResponse> peekQueueMessagesQuery,
            ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse> bulkCreateMessagesCommand,
            ICommandHandler<SendMessageToErrorQueueCommand, SendMessageToErrorQueueCommandResponse> sendMessageToErrorQueueCommand,
            IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery)
        {
            _logger = logger;
            _userService = userService;
            _getUserSessionQuery = getUserSessionQuery;
            _getQueuesQuery = getQueuesQuery;
            _peekQueueMessagesQuery = peekQueueMessagesQuery;
            _getMessagesQuery = getMessagesQuery;
            _bulkCreateMessagesCommand = bulkCreateMessagesCommand;
            _sendMessageToErrorQueueCommand = sendMessageToErrorQueueCommand;
        }

        public async Task<IActionResult> Index()
        {

#if DEBUG
            Debugger.Break();
#endif
            var response = await _getUserSessionQuery.Handle(new GetUserSessionQuery()
                {
                    UserId = _userService.GetUserId()
                });

            if (response.UserHasExistingSession)
            {
                return RedirectToAction(actionName: "Index", controllerName: "MessageList");
            }

            var searchVM = new QueueViewModel
            {
                Queues = (await _getQueuesQuery.Handle(new GetQueuesQuery())).Queues
            };

            return View(searchVM);
        }

#if DEBUG
        public async Task<IActionResult> ImportToCosmos(string queueName = null)
        {
            queueName ??= "sfa.das.notifications.messagehandlers-errors";

            var response = await _peekQueueMessagesQuery.Handle(new PeekQueueMessagesQuery()
                { 
                    QueueName = queueName
                });

            await _bulkCreateMessagesCommand.Handle(new BulkCreateQueueMessagesCommand()
                { 
                    Messages = response.Messages
            });

            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }

        public async Task<IActionResult> ImportToQueue(string queueName = null)
        {
            var response = await _getMessagesQuery.Handle(new GetMessagesQuery()
                {
                    UserId = "123456",
                    SearchProperties = new SearchProperties()
                });
            
            foreach (var msg in response.Messages)
            {
                await _sendMessageToErrorQueueCommand.Handle(new SendMessageToErrorQueueCommand()
                    {
                        Message = msg
                    });
            }

            return RedirectToAction(actionName: "Index", controllerName: "Home");
        }
#endif

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
