using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;
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

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers;

[ServiceFilter(typeof(KeepUserSessionActiveFilter))]
public class MessageListController(
    IUserService userService,
    IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
    IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse> getMessagesByIdQuery,
    IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse> getQueueDetailsQuery,
    IMessageService messageService,
    IUserSessionService userSessionService,
    IOptions<ServiceBusErrorManagementSettings> settings,
    ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse> deleteQueueMessageCommand,
    IRetrieveMessagesService retrieveMessagesService,
    IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse> getQueueMessageCountQuery)
    : Controller
{
    private readonly ServiceBusErrorManagementSettings _settings = settings.Value;

    public async Task<IActionResult> Index(int returnOffset = 0, int returnLimit = 10, string returnSearch = null,
        string returnSort = null, string returnOrder = null, int returnGetQuantity = 250)
    {
        var response = await getMessagesQuery.Handle(new GetMessagesQuery()
        {
            UserId = userService.GetUserId(),
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
            QueueInfo = (await getQueueDetailsQuery.Handle(new GetQueueDetailsQuery()
            {
                QueueName = queueName
            })).QueueInfo,
            UserSession = await userSessionService.GetUserSession(),
            ListState = BuildReturnState(returnOffset, returnLimit, returnSearch, returnSort, returnOrder, returnGetQuantity)
        });
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveMessagesFromQueue(ReceiveMessagesModel model)
    {
        try
        {
            var count = (await getQueueMessageCountQuery.Handle(new GetQueueMessageCountQuery()
            {
                QueueName = model.QueueName
            })).Count;

            await retrieveMessagesService.GetMessages(model.QueueName, count, model.GetQuantity);

            // Will always update session on receiving messages but removes reliance on HttpContext Session
            await userSessionService.UpsertUserSession(model.QueueName);

            return RedirectToAction("Index", BuildRouteValues(model));
        }
        catch
        {
            TempData["ErrorMessage"] = "Could not retrieve additional messages. Please try again.";
            return RedirectToAction("Index", BuildRouteValues(model));
        }
    }

    [HttpPost]
    public async Task<IActionResult> ReleaseSelectedMessages(ReleaseSelectedMessages model)
    {
        try
        {
            var response = await getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
            {
                UserId = userService.GetUserId(),
                Ids = model.Ids?.Split(",")
            });

            await messageService.AbortMessages(response.Messages, model.QueueName);
            return RedirectToAction("Index", BuildRouteValues(model));
        }
        catch
        {
            TempData["ErrorMessage"] = "Could not release the selected messages. Please try again.";
            return RedirectToAction("Index", BuildRouteValues(model));
        }
    }

    public async Task<IActionResult> ReleaseAllMessages(string queue)
    {
        var response = await getMessagesQuery.Handle(new GetMessagesQuery()
        {
            UserId = userService.GetUserId(),
            SearchProperties = new SearchProperties()
        });

        await messageService.AbortMessages(response.Messages, queue);

        return RedirectToAction("Index", "Servicebus");
    }

    [HttpPost]
    public async Task<IActionResult> ReplayMessages(ReplayMessagesModel model)
    {
        try
        {
            var response = await getMessagesByIdQuery.Handle(new GetMessagesByIdQuery()
            {
                UserId = userService.GetUserId(),
                Ids = model.Ids?.Split(",")
            });

            var processingQueueName = model.QueueName.GetProcessingQueueName(_settings.ErrorQueueRegex);
            await messageService.ReplayMessages(response.Messages, processingQueueName);

            return RedirectToAction("Index", BuildRouteValues(model));
        }
        catch
        {
            TempData["ErrorMessage"] = "Could not replay the selected messages. Please try again.";
            return RedirectToAction("Index", BuildRouteValues(model));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteMessages(DeleteMessagesModel model)
    {
        try
        {
            await deleteQueueMessageCommand.Handle(new BatchDeleteQueueMessagesCommand()
            {
                Ids = model.Ids?.Split(",")
            });

            return RedirectToAction("Index", BuildRouteValues(model));
        }
        catch
        {
            TempData["ErrorMessage"] = "Could not delete the selected messages. Please try again.";
            return RedirectToAction("Index", BuildRouteValues(model));
        }
    }

    private async Task DeleteUserSession()
    {
        await userSessionService.DeleteUserSession();
        HttpContext.Session.Set<DateTime?>("sessionActiveUntil", null);
    }

    private static MessageListState BuildReturnState(int returnOffset, int returnLimit, string returnSearch,
        string returnSort, string returnOrder, int returnGetQuantity)
    {
        return new MessageListState
        {
            ReturnOffset = returnOffset < 0 ? 0 : returnOffset,
            ReturnLimit = returnLimit <= 0 ? 10 : returnLimit,
            ReturnSearch = returnSearch,
            ReturnSort = returnSort,
            ReturnOrder = returnOrder,
            ReturnGetQuantity = returnGetQuantity <= 0 ? 250 : returnGetQuantity
        };
    }

    private static object BuildRouteValues(ReplayMessagesModel model) => BuildRouteValues(model.ReturnOffset,
        model.ReturnLimit, model.ReturnSearch, model.ReturnSort, model.ReturnOrder, model.ReturnGetQuantity);

    private static object BuildRouteValues(DeleteMessagesModel model) => BuildRouteValues(model.ReturnOffset,
        model.ReturnLimit, model.ReturnSearch, model.ReturnSort, model.ReturnOrder, model.ReturnGetQuantity);

    private static object BuildRouteValues(ReceiveMessagesModel model) => BuildRouteValues(model.ReturnOffset,
        model.ReturnLimit, model.ReturnSearch, model.ReturnSort, model.ReturnOrder, model.ReturnGetQuantity);

    private static object BuildRouteValues(ReleaseSelectedMessages model) => BuildRouteValues(model.ReturnOffset,
        model.ReturnLimit, model.ReturnSearch, model.ReturnSort, model.ReturnOrder, model.ReturnGetQuantity);

    private static Dictionary<string, object> BuildRouteValues(int offset, int limit, string search, string sort,
        string order, int getQuantity)
    {
        return new Dictionary<string, object>
        {
            ["returnOffset"] = offset,
            ["returnLimit"] = limit,
            ["returnSearch"] = search,
            ["returnSort"] = sort,
            ["returnOrder"] = order,
            ["returnGetQuantity"] = getQuantity
        };
    }
}