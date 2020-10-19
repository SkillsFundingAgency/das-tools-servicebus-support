namespace SFA.DAS.Tools.Servicebus.Support.Domain
{
    public class UserMessageCount
    {
        public string UserId { get; set; }
        public string Queue { get; set; }
        public int MessageCount { get; set; }
    }
}
