namespace SFA.DAS.Tools.Servicebus.Support.Application.Services;

public class ReplayDestinationRequest
{
    public bool Confirmed { get; set; }
    public ReplayDestinationKind Kind { get; set; }
    public string Destination { get; set; }
    public string Confirmation { get; set; }
    public string SourceQueue { get; set; }
}
