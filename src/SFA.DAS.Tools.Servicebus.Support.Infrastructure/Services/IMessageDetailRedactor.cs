using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public interface IMessageDetailRedactor
    {
        IEnumerable<KeyValuePair<string, string>> Redact(IEnumerable<KeyValuePair<string, object>> values);
    }
}
