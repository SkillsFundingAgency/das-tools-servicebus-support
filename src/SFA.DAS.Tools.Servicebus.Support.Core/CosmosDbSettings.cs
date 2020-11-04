namespace SFA.DAS.Tools.Servicebus.Support.Domain
{
    public class CosmosDbSettings
    {
        public const string CosmosDbSettingsKey = "CosmosDbSettings";
        public string CollectionName { get; set; }
        public string Url { get; set; }
        public string DatabaseName { get; set; }
        public int Throughput { get; set; }
        public string AuthKey { get; set; }
        public int DefaultCosmosInterimRequestTimeout { get; set; }
        public int DefaultCosmosOperationTimeout { get; set; }
    }
}
