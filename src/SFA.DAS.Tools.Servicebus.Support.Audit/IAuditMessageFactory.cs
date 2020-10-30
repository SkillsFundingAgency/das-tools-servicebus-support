using SFA.DAS.Tools.Servicebus.Support.Audit.Types;

namespace SFA.DAS.Tools.Servicebus.Support.Audit
{
    public interface IAuditMessageFactory
    {
        AuditMessage Build();
    }
}