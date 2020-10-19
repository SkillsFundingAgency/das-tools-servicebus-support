using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;
using System;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public class KeepUserSessionActiveFilter : IResultFilter
    {
        private readonly IUserSessionService _userSessionService;
        private readonly int _userSessionRefreshIntervalMinutes;

        public KeepUserSessionActiveFilter(IUserSessionService userSessionService, IConfiguration config)
        {
            _userSessionService = userSessionService;
            _userSessionRefreshIntervalMinutes = config.GetValue<int>("UserRefreshSessionIntervalMinutes");
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {

        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            var sessionActiveUntil = context.HttpContext.Session.Get<DateTime?>("sessionActiveUntil");

            if (!sessionActiveUntil.HasValue || sessionActiveUntil < DateTime.UtcNow)
            {
                var deleteSession = context.HttpContext.Session.Get<bool>("deleteSession");

                if (deleteSession)
                {
                    context.HttpContext.Session.Set("deleteSession", false);
                    return;
                }

                var queue = context.HttpContext.Session.GetString("queueName");
                if (!string.IsNullOrEmpty(queue))
                {
                    _userSessionService.UpsertUserSession(queue).Wait();
                    var sessionExpiry = DateTime.UtcNow.AddMinutes(_userSessionRefreshIntervalMinutes);
                    context.HttpContext.Session.Set("sessionActiveUntil", sessionExpiry);
                }
            }
        }
    }
}
