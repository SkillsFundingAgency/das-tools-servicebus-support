using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;
using System;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public class KeepUserSessionActiveFilter : IActionFilter
    {
        private readonly IUserSessionService _userSessionService;
        private readonly int _userSessionRefreshIntervalMinutes;

        public KeepUserSessionActiveFilter(IUserSessionService userSessionService, IConfiguration config)
        {
            _userSessionService = userSessionService;
            _userSessionRefreshIntervalMinutes = config.GetValue<int>("UserRefreshSessionIntervalMinutes");
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var sessionActiveUntil = context.HttpContext.Session.Get<DateTime?>("sessionActiveUntil");

            if (!sessionActiveUntil.HasValue || sessionActiveUntil < DateTime.UtcNow)
            {                
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
