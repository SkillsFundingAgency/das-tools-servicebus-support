using System.Text;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.UnitTests.Extensions;

public class WhenConvertingMessageWithMissingNServiceBusHeaders
{
    [Test]
    public void ThenItUsesUnknownOriginatingEndpointAndDoesNotThrow()
    {
        // Arrange
        var message = new Message(Encoding.UTF8.GetBytes("{}"))
        {
            MessageId = "id-1"
        };

        message.UserProperties.Add("NServiceBus.ProcessingEndpoint", "processing");
        message.UserProperties.Add("NServiceBus.ExceptionInfo.Message", "Exception Message");
        message.UserProperties.Add("NServiceBus.ExceptionInfo.ExceptionType", "Exception Type");

        // Act
        var converted = message.Convert("user-1", "queue-1");

        // Assert
        converted.OriginatingEndpoint.Should().Be("(unknown)");
        converted.ProcessingEndpoint.Should().Be("processing");
        converted.Exception.Should().Be("Exception Message");
        converted.ExceptionType.Should().Be("Exception Type");
    }

    [Test]
    public void ThenItUsesEmptyFallbacksForOtherMissingHeaders()
    {
        // Arrange
        var message = new Message(Encoding.UTF8.GetBytes("{}"))
        {
            MessageId = "id-2"
        };

        // Act
        var converted = message.Convert("user-1", "queue-1");

        // Assert
        converted.OriginatingEndpoint.Should().Be("(unknown)");
        converted.ProcessingEndpoint.Should().BeEmpty();
        converted.Exception.Should().BeEmpty();
        converted.ExceptionType.Should().BeEmpty();
    }
}