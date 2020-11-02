using Microsoft.AspNetCore.Http;
using SFA.DAS.Audit.Client;
using SFA.DAS.Tools.Servicebus.Support.Audit.MessageBuilders;
using System.Reflection;
using System.Security.Claims;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public class WebMessageBuilders : IWebMessageBuilders
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public static string UserIdClaim = ClaimTypes.NameIdentifier;
        public static string UserEmailClaim = ClaimTypes.Email;

        public WebMessageBuilders(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Register()
        {
            AuditMessageFactory.RegisterBuilder(message =>
            {
                new ChangedByMessageBuilder(_httpContextAccessor).Build(message);
            });

            AuditMessageFactory.RegisterBuilder(message =>
            {
                var name = Assembly.GetExecutingAssembly();

                message.Source = new Audit.Types.Source
                {
                    System = "SFA.DAS.Tools.Servicebus.Support.Web",
                    Component = name.GetName().Name,
                    Version = name.GetName().Version.ToString()
                };
            });
        }
    }
}
