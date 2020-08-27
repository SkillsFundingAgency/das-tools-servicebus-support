using System;

namespace SFA.DAS.Tools.Servicebus.Support.Core
{
    public class sbMessageModel
    {
        
        public Guid id { get; set; }
        public string UserId { get; set; }

        public string MessageId { get; set; }
        public string TimeOfFailure { get; set; }
        public string ExceptionType { get; set; }
        public string OriginatingEndpoint { get; set; }
        public string ProcessingEndpoint { get; set; }
        public string EnclosedMessageTypes { get; set; }
        public string ExceptionMessage { get; set; }
        public object StackTrace { get; set; }
        public string RawMessage { get; set; }
    }
}
