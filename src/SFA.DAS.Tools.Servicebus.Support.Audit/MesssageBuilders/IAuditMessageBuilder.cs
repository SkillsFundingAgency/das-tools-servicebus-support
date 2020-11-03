using SFA.DAS.Tools.Servicebus.Support.Audit.Types;

namespace SFA.DAS.Tools.Servicebus.Support.Audit.MessageBuilders
{
    public interface IAuditMessageBuilder
    {
        void Build(AuditMessage message);
    }
}