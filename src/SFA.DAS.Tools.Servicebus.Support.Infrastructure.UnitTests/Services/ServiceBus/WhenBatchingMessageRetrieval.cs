using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.UnitTests.Services.ServiceBus
{
    public class WhenBatchingMessageRetrieval
    {
        private const string QueueName = "q";
        private const int Expected = 30;
        private const int BatchSize = 10;
        private const int TotalOnQueue = 30;
        private int _totalRetrieved = 0;

        [Test]
        public async Task ThenMessagesAreRetrievedInCorrectBatchSizes()
        {
            var strategy = new BatchGetMessageStrategy();
            var counter = 0;

            var messages = await strategy.Execute<Message, Message>(QueueName, Expected, BatchSize, async (x) =>
            {
                counter++;
                var retVal = new List<Message>();
                var i = 0;

                while (i++ < x && _totalRetrieved < TotalOnQueue)
                {
                    retVal.Add(new Message());
                    _totalRetrieved++;
                }

                return retVal;

            }, async (msg) => msg);

            messages.Count.Should().Be(Expected);
            counter.Should().Be(3);
        }
    }
}
