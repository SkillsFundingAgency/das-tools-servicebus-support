namespace SFA.DAS.Tools.Servicebus.Support.Web.Models;

public class ReplayMessagesToDestinationModel
{
    public string Ids { get; set; }
    public string QueueName { get; set; }
    public string DestinationName { get; set; }
    public string DestinationKind { get; set; }
    public string ConfirmationName { get; set; }
    public bool Confirmed { get; set; }
    public int ReturnOffset { get; set; }
    public int ReturnLimit { get; set; } = 10;
    public string ReturnSearch { get; set; }
    public string ReturnSort { get; set; }
    public string ReturnOrder { get; set; }
    public int ReturnGetQuantity { get; set; } = 250;
}
