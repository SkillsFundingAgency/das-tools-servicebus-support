using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.ServiceBus;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions
{
    public static class MessageExtensions
    {
        public static long GetEstimatedMessageSize(this Message message)
        {
            const int messageSizePaddingPercentage = 5;
            const int assumeSize = 256;
            var standardPropertiesSize = GetStringSizeInBytes(message.MessageId) +
                                         assumeSize + // ContentType
                                         assumeSize + // CorrelationId
                                         4 + // DeliveryCount
                                         8 + // EnqueuedSequenceNumber
                                         8 + // EnqueuedTimeUtc
                                         8 + // ExpiresAtUtc
                                         1 + // ForcePersistence
                                         1 + // IsBodyConsumed
                                         assumeSize + // Label
                                         8 + // LockedUntilUtc 
                                         16 + // LockToken 
                                         assumeSize + // PartitionKey
                                         8 + // ScheduledEnqueueTimeUtc
                                         8 + // SequenceNumber
                                         assumeSize + // SessionId
                                         4 + // State
                                         8 + // TimeToLive
                                         assumeSize + // To
                                         assumeSize;  // ViaPartitionKey;

            var headers = message.UserProperties.Sum(property => GetStringSizeInBytes(property.Key) + GetStringSizeInBytes(property.Value?.ToString()));
            var bodySize = message.Body.Length;
            var total = standardPropertiesSize + headers + bodySize;

            var padWithPercentage = (double)(100 + messageSizePaddingPercentage) / 100;
            var estimatedSize = (long)(total * padWithPercentage);
            
            return estimatedSize;
        }

        public static QueueMessage Convert(this Message message, string userId, string queueName)
        {
            return new QueueMessage
            {
                Id = message.MessageId,
                UserId = userId,
                OriginalMessage = message,
                Queue = queueName,
                IsReadOnly = false,
                Body = Encoding.UTF8.GetString(message.Body),
                OriginatingEndpoint = message.UserProperties["NServiceBus.OriginatingEndpoint"].ToString(),
                ProcessingEndpoint = message.UserProperties["NServiceBus.ProcessingEndpoint"].ToString(),
                Exception = message.UserProperties["NServiceBus.ExceptionInfo.Message"].ToString(),
                ExceptionType = message.UserProperties["NServiceBus.ExceptionInfo.ExceptionType"].ToString()
            };
        }

        public static string GetLockToken(this Message message)
        {
            return message.SystemProperties.IsLockTokenSet ? message.SystemProperties.LockToken : string.Empty;
        }

        private static int GetStringSizeInBytes(string value) => value != null ? Encoding.UTF8.GetByteCount(value) : 0;
    }
}
