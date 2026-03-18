using SFA.DAS.Tools.Servicebus.Support.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services;

public interface IOrphanedMessageRecoveryService
{
    Task RecoverOrphanedMessagesAsync(IEnumerable<UserSession> activeSessions, int maxIterations = 200);
}