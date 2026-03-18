using System.Collections.Generic;

namespace SFA.DAS.Tools.Servicebus.Support.Web.Models;

public class MessageDetailViewModel
{
    public string Queue { get; set; }
    public string Body { get; set; }
    public IEnumerable<KeyValuePair<string, string>> UserProperties { get; set; }
    public IEnumerable<KeyValuePair<string, string>> Properties { get; set; }
    public MessageListState ReturnState { get; set; } = new();
}