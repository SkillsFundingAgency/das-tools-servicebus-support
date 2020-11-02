using SFA.DAS.Tools.Servicebus.Support.Audit.Types;

namespace SFA.DAS.Tools.Servicebus.Support.Audit.MessageBuilders
{
    internal interface IChangedByMessageBuilder
    {
        void Build(AuditMessage message);
    }
}