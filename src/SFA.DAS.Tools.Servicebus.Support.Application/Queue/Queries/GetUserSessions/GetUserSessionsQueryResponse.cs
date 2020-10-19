using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSessions
{
    public class GetUserSessionsQueryResponse
    {
        public IEnumerable<UserSession> UserSessions { get; set; }
    }
}
