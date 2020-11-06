namespace SFA.DAS.Tools.Servicebus.Support.Domain.Configuration
{
    public class ServiceBusErrorManagementSettings
    {
        public const string ServiceBusErrorManagementSettingsKey = "ServiceBusSettings";
        public string ServiceBusConnectionString { get; set; }
        public int PeekMessageBatchSize { get; set; }
        public string QueueSelectionRegex { get; set; }
        public string ErrorQueueRegex { get; set; }
        public string[] RedactPatterns { get; set; }

    }
}
