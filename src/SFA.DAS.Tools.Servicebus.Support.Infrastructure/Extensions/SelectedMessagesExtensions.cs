using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SFA.DAS.Tools.Servicebus.Support.Domain;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class SelectedMessagesExtensions
    {
        public static string GetProcessingQueueName(this string queue, string regex, string replacementString = "") => Regex.Replace(queue, regex, replacementString);
    }
}
