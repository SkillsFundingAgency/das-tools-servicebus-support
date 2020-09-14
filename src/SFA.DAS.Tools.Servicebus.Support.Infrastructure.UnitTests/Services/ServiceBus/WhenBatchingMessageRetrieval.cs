using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Infrastructure.UnitTests.Services.ServiceBus
{
    public class WhenBatchingMessageRetrieval
    {
        private readonly string _queueName = "q";
        private readonly int expected = 30;
        private readonly int batchSize = 10;
        private readonly int totalOnQueue = 30;
        private int _totalRetrieved = 0;

        [Test]
        public async Task ThenMessagesAreRetrievedInCorrectBatchSizes()
        {
            var strategy = new BatchMessageStrategy();
            var counter = 0;

            var messages = await strategy.Execute<Message, Message>(_queueName, expected, batchSize, async (x) =>
            {
                counter++;
                var retVal = new List<Message>();
                var i = 0;

                while (i++ < x && _totalRetrieved < totalOnQueue)
                {
                    retVal.Add(new Message());
                    _totalRetrieved++;
                }

                return retVal;

            }, async (msg) => msg);

            messages.Count.Should().Be(expected);
            counter.Should().Be(3);
        }
    }
}
