namespace SFA.DAS.Tools.Servicebus.Support.Web.Models;

public class MessageListState
{
    public int ReturnOffset { get; set; }
    public int ReturnLimit { get; set; } = 10;
    public string ReturnSearch { get; set; }
    public string ReturnSort { get; set; }
    public string ReturnOrder { get; set; }
    public int ReturnGetQuantity { get; set; } = 250;
}