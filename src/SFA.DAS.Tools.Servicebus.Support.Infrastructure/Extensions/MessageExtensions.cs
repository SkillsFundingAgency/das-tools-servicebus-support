using System.Linq;
using System.Text;
using Microsoft.Azure.ServiceBus;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;

public static class MessageExtensions
{
    private const string UnknownEndpoint = "(unknown)";

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
            OriginatingEndpoint = GetUserPropertyString(message, "NServiceBus.OriginatingEndpoint", UnknownEndpoint),
            ProcessingEndpoint = GetUserPropertyString(message, "NServiceBus.ProcessingEndpoint", string.Empty),
            Exception = GetUserPropertyString(message, "NServiceBus.ExceptionInfo.Message", string.Empty),
            ExceptionType = GetUserPropertyString(message, "NServiceBus.ExceptionInfo.ExceptionType", string.Empty)
        };
    }

    public static string GetLockToken(this Message message)
    {
        return message.SystemProperties.IsLockTokenSet ? message.SystemProperties.LockToken : string.Empty;
    }

    private static string GetUserPropertyString(Message message, string key, string fallback)
    {
        if (message?.UserProperties == null)
        {
            return fallback;
        }

        return message.UserProperties.TryGetValue(key, out var value) && value != null
            ? value.ToString()
            : fallback;
    }

    private static int GetStringSizeInBytes(string value) => value != null ? Encoding.UTF8.GetByteCount(value) : 0;
}