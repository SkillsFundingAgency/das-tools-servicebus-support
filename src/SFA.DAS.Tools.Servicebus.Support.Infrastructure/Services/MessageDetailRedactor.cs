using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services
{
    public class MessageDetailRedactor : IMessageDetailRedactor
    {
        private readonly IEnumerable<string> _tokens;
        private readonly string _redacted = "[REDACTED]";

        public MessageDetailRedactor(ServiceBusErrorManagementSettings serviceBusSettings)
        {
            _tokens = serviceBusSettings.RedactPatterns;
        }

        public IEnumerable<KeyValuePair<string, string>> Redact(IEnumerable<KeyValuePair<string, object>> values)
        {
            var result = new ConcurrentDictionary<string, string>();

            Parallel.ForEach(values, (value) =>
            {
                foreach (var token in _tokens)
                {
                    if (value.Value == null)
                    {
                        result.AddOrUpdate(value.Key, string.Empty, (x, y) => y);

                        continue;
                    }

                    result.AddOrUpdate(value.Key, Regex.Replace(value.Value.ToString(), token, _redacted), (x, y) => y);
                }
            });

            return result.ToList();
        }
    }
}
 