using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;
using System;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public class KeepUserSessionActiveFilter : IActionFilter
    {
        private readonly IUserSessionService _userSessionService;
        private readonly int _userSessionRefreshIntervalMinutes;
        private readonly ILogger<KeepUserSessionActiveFilter> _logger;

        public KeepUserSessionActiveFilter(IUserSessionService userSessionService, UserIdentitySettings userIdentitySettings, ILogger<KeepUserSessionActiveFilter> logger)
        {
            _userSessionService = userSessionService;
            _userSessionRefreshIntervalMinutes = userIdentitySettings.UserRefreshSessionIntervalMinutes;
            _logger = logger;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _logger.LogWarning("KeepUserSessionActiveFilter: Entering OnActionExecuting KeepUserSessionActiveFilter");
            var sessionActiveUntil = context.HttpContext.Session.Get<DateTime?>("sessionActiveUntil");
            _logger.LogWarning($"KeepUserSessionActiveFilter: Session Active Until {sessionActiveUntil}");
            if (!sessionActiveUntil.HasValue || sessionActiveUntil < DateTime.UtcNow)
            {
                _logger.LogWarning($"KeepUserSessionActiveFilter: Entering If");

                var queue = context.HttpContext.Session.GetString("queueName");

                _logger.LogWarning($"KeepUserSessionActiveFilter: QueueName Found: '{queue}'");

                if (!string.IsNullOrEmpty(queue))
                {
                    _logger.LogWarning($"KeepUserSessionActiveFilter: QueueNameNotEmpty");

                    _userSessionService.UpsertUserSession(queue).Wait();
                    _logger.LogWarning($"KeepUserSessionActiveFilter: Creationg Session");
                    var sessionExpiry = DateTime.UtcNow.AddMinutes(_userSessionRefreshIntervalMinutes);
                    _logger.LogWarning($"KeepUserSessionActiveFilter: Calculating Expiry As {sessionExpiry}");
                    context.HttpContext.Session.Set("sessionActiveUntil", sessionExpiry);
                    _logger.LogWarning($"KeepUserSessionActiveFilter: Set Session Expiry {sessionExpiry}");
                }
            }
            else
            {
                _logger.LogWarning($"KeepUserSessionActiveFilter: Did not enter If statement");
            }

        }            
    }
}
