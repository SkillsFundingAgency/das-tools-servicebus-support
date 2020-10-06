using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions
{
    public class GetExpiredUserSessionsQueryResponse
    {
        public IEnumerable<UserSession> ExpiredUserSessions { get; set; }
    }
}
