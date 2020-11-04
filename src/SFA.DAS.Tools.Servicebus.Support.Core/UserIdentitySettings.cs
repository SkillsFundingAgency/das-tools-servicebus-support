namespace SFA.DAS.Tools.Servicebus.Support.Domain
{
    public class UserIdentitySettings
    {
        public const string UserIdentitySettingsKey = "UserIdentitySettings";
        public string NameClaim { get; set; }
        public string RequiredRole { get; set; }
        public int UserSessionExpiryHours { get; set; }
        public int UserRefreshSessionIntervalMinutes { get; set; }
    }
}
