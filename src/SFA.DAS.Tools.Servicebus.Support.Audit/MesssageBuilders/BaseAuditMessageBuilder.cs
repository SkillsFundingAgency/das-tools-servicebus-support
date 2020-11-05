using SFA.DAS.Tools.Servicebus.Support.Audit.MessageBuilders;
using SFA.DAS.Tools.Servicebus.Support.Audit.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SFA.DAS.Tools.Servicebus.Support.Audit.MesssageBuilders
{
    public class BaseAuditMessageBuilder : IAuditMessageBuilder
    {
        public void Build(AuditMessage message)
        {
            var name = Assembly.GetExecutingAssembly();

            message.Source = new Audit.Types.Source
            {
                System = "SFA.DAS.Tools.Servicebus.Support.Web",
                Component = name.GetName().Name,
                Version = name.GetName().Version.ToString()
            };

            message.ChangeAt = DateTime.UtcNow;
        }
    }
}
