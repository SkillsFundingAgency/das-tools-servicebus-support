using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public interface IAuditService
    {
        Task WriteAudit(AuditMessage message);
    }
}