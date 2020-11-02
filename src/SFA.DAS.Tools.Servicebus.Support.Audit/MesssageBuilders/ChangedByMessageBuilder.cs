using Microsoft.AspNetCore.Http;
using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System.Linq;

namespace SFA.DAS.Tools.Servicebus.Support.Audit.MessageBuilders
{
    internal class ChangedByMessageBuilder : IChangedByMessageBuilder
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChangedByMessageBuilder(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public void Build(AuditMessage message)
        {
            message.ChangedBy = new Actor();
            SetOriginIpAddess(message.ChangedBy);
            SetUserIdAndEmail(message.ChangedBy);
        }

        private void SetOriginIpAddess(Actor actor)
        {
            actor.OriginIpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString() == "::1"
                ? "127.0.0.1"
                : _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
        }

        private void SetUserIdAndEmail(Actor actor)
        {
            var user = _httpContextAccessor.HttpContext.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
            {
                return;
            }

            if (!string.IsNullOrEmpty(WebMessageBuilders.UserIdClaim))
            {
                var claim = user.Claims.FirstOrDefault(c => c.Type.Equals(WebMessageBuilders.UserIdClaim, System.StringComparison.CurrentCultureIgnoreCase));
                if (claim == null)
                {
                    throw new InvalidContextException($"User does not have claim {WebMessageBuilders.UserIdClaim} to populate AuditMessage.ChangedBy.Id");
                }
                actor.Id = claim.Value;
            }

            if (!string.IsNullOrEmpty(WebMessageBuilders.UserEmailClaim))
            {
                var claim = user.Claims.FirstOrDefault(c => c.Type.Equals(WebMessageBuilders.UserEmailClaim, System.StringComparison.CurrentCultureIgnoreCase));
                if (claim == null)
                {
                    throw new InvalidContextException($"User does not have claim {WebMessageBuilders.UserEmailClaim} to populate AuditMessage.ChangedBy.EmailAddress");
                }
                actor.EmailAddress = claim.Value;
            }
        }
    }
}
