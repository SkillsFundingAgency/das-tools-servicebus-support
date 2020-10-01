using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SFA.DAS.Tools.Servicebus.Support.Domain;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class SelectedMessagesExtensions
    {
        public static string GetProcessingQueueName(this SelectedMessages messages, string regex, string replacementString = "") => Regex.Replace(messages.Queue, regex, replacementString);
    }
}
