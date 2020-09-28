using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Web.Models;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Controllers
{
    public class MessageDetailsController : Controller
    {
        private readonly IQueryHandler<GetMessageQuery, GetMessageQueryResponse> _getMessageQuery;
        private readonly IUserService _userService;
        private readonly IMessageDetailRedactor _redactor;

        public MessageDetailsController(IQueryHandler<GetMessageQuery, GetMessageQueryResponse> getMessageQuery, IUserService userService, IMessageDetailRedactor redactor)
        {
            _getMessageQuery = getMessageQuery;
            _userService = userService;
            _redactor = redactor;
        }

        public async Task<IActionResult> Index(string id)
        {
            var message = await _getMessageQuery.Handle(new GetMessageQuery()
                {
                    UserId = _userService.GetUserId(),
                    MessageId = id

                });

            return View(new MessageDetailViewModel
            {
                Queue = message.Message.Body,
                Body = message.Message.Queue,
                Properties = _redactor.Redact(ConvertPropertiesToList(message)).OrderBy(x => x.Key),
                UserProperties = _redactor.Redact(message.Message.UserProperties).OrderBy(x => x.Key)
            });
        }

        private static List<KeyValuePair<string, object>> ConvertPropertiesToList(GetMessageQueryResponse message)
        {
            var properties = new List<KeyValuePair<string, object>>()
            {
                new KeyValuePair<string, object>("ContentType", message.Message.OriginalMessage.ContentType ?? string.Empty),
                new KeyValuePair<string, object>("CorrelationId",
                    message.Message.OriginalMessage.CorrelationId ?? string.Empty),
                new KeyValuePair<string, object>("Label", message.Message.OriginalMessage.Label ?? string.Empty),
                new KeyValuePair<string, object>("MessageId", message.Message.OriginalMessage.MessageId ?? string.Empty),
                new KeyValuePair<string, object>("PartitionKey", message.Message.OriginalMessage.PartitionKey ?? string.Empty),
                new KeyValuePair<string, object>("ReplyTo", message.Message.OriginalMessage.ReplyTo ?? string.Empty),
                new KeyValuePair<string, object>("ReplyToSessionId",
                    message.Message.OriginalMessage.ReplyToSessionId ?? string.Empty),
                new KeyValuePair<string, object>("Size", message.Message.OriginalMessage.Size.ToString()),
                new KeyValuePair<string, object>("TimeToLive", message.Message.OriginalMessage.TimeToLive.ToString()),
                new KeyValuePair<string, object>("To", message.Message.OriginalMessage.To ?? string.Empty),
                new KeyValuePair<string, object>("ViaPartitionKey",
                    message.Message.OriginalMessage.ViaPartitionKey ?? string.Empty),
                new KeyValuePair<string, object>("ScheduledEnqueueTimeUtc",
                    message.Message.OriginalMessage.ScheduledEnqueueTimeUtc.ToString(CultureInfo.CurrentCulture)),
                new KeyValuePair<string, object>("SessionId", message.Message.OriginalMessage.SessionId ?? string.Empty)
            };
            return properties;
        }
    }
}
