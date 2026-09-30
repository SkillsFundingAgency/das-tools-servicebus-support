namespace SFA.DAS.Tools.Servicebus.Support.Web.Models;

public class ReplayAllBatchResponse
{
    public int Processed { get; set; }
    public int Remaining { get; set; }
    public bool Done { get; set; }
    public string Error { get; set; }
}
