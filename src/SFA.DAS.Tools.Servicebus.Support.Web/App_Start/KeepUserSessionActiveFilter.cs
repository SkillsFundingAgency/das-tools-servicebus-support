using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using System;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public class KeepUserSessionActiveFilter : IResultFilter
    {
        private readonly IUserSessionService _userSessionService;
        private readonly int _userSessionRefreshIntervalMinutes;
        private DateTime _lastUpdated;

        public KeepUserSessionActiveFilter(IUserSessionService userSessionService, IConfiguration config)
        {
            _userSessionService = userSessionService;
            _userSessionRefreshIntervalMinutes = config.GetValue<int>("UserRefreshSessionIntervalMinutes");            
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
            
        }

        public async void OnResultExecuting(ResultExecutingContext context)
        {
            if (_lastUpdated < DateTime.UtcNow)
            {
                var queue = context.HttpContext.Session.GetString("queueName");
                if (!string.IsNullOrEmpty(queue))
                {
                    await _userSessionService.UpsertUserSession(queue);
                    _lastUpdated = DateTime.UtcNow.AddMinutes(_userSessionRefreshIntervalMinutes);
                }                
            }            
        }
    }
}
