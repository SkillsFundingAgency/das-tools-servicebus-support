using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Services.ReplayDestination
{
    public class WhenValidatingAReplayDestination
    {
        [Test]
        public void ThenAnUnconfirmedRequestIsRejected()
        {
            var result = ReplayDestinationValidator.Validate(new ReplayDestinationRequest
            {
                Confirmed = false,
                Kind = ReplayDestinationKind.Queue,
                Destination = "sfa.das.commitmentsv2.messagehandlers",
                Confirmation = "sfa.das.commitmentsv2.messagehandlers",
                SourceQueue = "sfa.das.commitmentsv2.externalhandlers-errors"
            });

            Assert.That(result, Is.EqualTo(ReplayDestinationValidator.ConfirmationRequired));
        }

        [Test]
        public void ThenAMissingKindIsRejected()
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest(kind: ReplayDestinationKind.Unspecified));

            Assert.That(result, Is.EqualTo(ReplayDestinationValidator.KindRequired));
        }

        [Test]
        public void ThenAnEmptyDestinationIsRejected()
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest(destination: "  ", confirmation: "  "));

            Assert.That(result, Is.EqualTo(ReplayDestinationValidator.DestinationRequired));
        }

        [TestCase("bundle 1")]
        [TestCase("bundle/1")]
        [TestCase("-bundle")]
        [TestCase("bundle-")]
        public void ThenAnInvalidNameIsRejected(string destination)
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest(destination: destination, confirmation: destination));

            Assert.That(result, Is.EqualTo(ReplayDestinationValidator.DestinationInvalid));
        }

        [Test]
        public void ThenAMismatchedConfirmationIsRejected()
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest(confirmation: "bundle-2"));

            Assert.That(result, Is.EqualTo(ReplayDestinationValidator.ConfirmationMismatch));
        }

        [Test]
        public void ThenTheSourceErrorQueueIsRejected()
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest(
                destination: "sfa.das.commitmentsv2.externalhandlers-errors",
                confirmation: "sfa.das.commitmentsv2.externalhandlers-errors"));

            Assert.That(result, Is.EqualTo(ReplayDestinationValidator.SameAsSourceQueue));
        }

        [Test]
        public void ThenAConfirmedQueueDestinationIsAccepted()
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest());

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ThenAConfirmedTopicDestinationIsAcceptedWhenTheConfirmationDiffersOnlyByCase()
        {
            var result = ReplayDestinationValidator.Validate(ValidRequest(
                kind: ReplayDestinationKind.Topic,
                destination: " bundle-1 ",
                confirmation: "BUNDLE-1"));

            Assert.That(result, Is.Null);
        }

        private static ReplayDestinationRequest ValidRequest(
            ReplayDestinationKind kind = ReplayDestinationKind.Queue,
            string destination = "sfa.das.commitmentsv2.messagehandlers",
            string confirmation = "sfa.das.commitmentsv2.messagehandlers")
        {
            return new ReplayDestinationRequest
            {
                Confirmed = true,
                Kind = kind,
                Destination = destination,
                Confirmation = confirmation,
                SourceQueue = "sfa.das.commitmentsv2.externalhandlers-errors"
            };
        }
    }
}
