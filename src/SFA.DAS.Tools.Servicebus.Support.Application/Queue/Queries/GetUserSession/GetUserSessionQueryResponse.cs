using SFA.DAS.Tools.Servicebus.Support.Domain;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession
{
    public class GetUserSessionQueryResponse
    {
        public UserSession UserSession { get; set; }
        public bool HasSession()
        {
            return UserSession != null;
        }
    }
}
