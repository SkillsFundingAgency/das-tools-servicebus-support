using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageDetailsController : Controller
    {
        private readonly IQueryHandler<GetMessageQuery, GetMessageQueryResponse> _getMessageQuery;
        private readonly IUserService _userService;

        public MessageDetailsController(IQueryHandler<GetMessageQuery, GetMessageQueryResponse> getMessageQuery, IUserService userService)
        {
            _getMessageQuery = getMessageQuery;
            _userService = userService;
        }

        public async Task<IActionResult> Index(string id)
        {
            var message = await _getMessageQuery.Handle(new GetMessageQuery()
                {
                    UserId = _userService.GetUserId(),
                    MessageId = id

                });

            return View(message.Message);
        }
    }
}
