using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public class KeepUserSessionActiveFilter : IAsyncActionFilter
    {
        private readonly IUserSessionService _userSessionService;
        private readonly int _userSessionRefreshIntervalMinutes;

        public KeepUserSessionActiveFilter(IUserSessionService userSessionService, UserIdentitySettings userIdentitySettings)
        {
            _userSessionService = userSessionService;
            _userSessionRefreshIntervalMinutes = userIdentitySettings.UserRefreshSessionIntervalMinutes;
        }


        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var sessionActiveUntil = context.HttpContext.Session.Get<DateTime?>("sessionActiveUntil");
            if (!sessionActiveUntil.HasValue || sessionActiveUntil < DateTime.UtcNow)
            {
                var userSession = await _userSessionService.GetUserSession();
                if(userSession != null)
                {
                    // Update Session Expiry time once within the sessionActive window
                    await _userSessionService.UpsertUserSession(userSession.Queue);

                    // Set sessionActive window to prevent constant updating
                    var sessionExpiry = DateTime.UtcNow.AddMinutes(_userSessionRefreshIntervalMinutes);
                    context.HttpContext.Session.Set("sessionActiveUntil", sessionExpiry);
                }

            }

            await next();
        }
    }
}
