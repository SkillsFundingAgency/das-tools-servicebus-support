using FluentAssertions;
using Moq;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Queue.Queries.GetUserSession
{
    public class WhenGettingUserSession
    {
        private readonly string _userId = "1";
        private Mock<ICosmosDbContext> _cosmosDbContext;

        [Test]
        public async Task ThenWillGetUserSessionFromService()
        {
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.HasUserAnExistingSession(_userId)).ReturnsAsync(true);

            var sut = new GetUserSessionQueryHandler(_cosmosDbContext.Object);

            await sut.Handle(new GetUserSessionQuery()
            {
                UserId = _userId
            });

            _cosmosDbContext.Verify(x => x.HasUserAnExistingSession(_userId), Times.Once);
        }

        [Test]
        public async Task AndTheResponseWillBeValid()
        {
            _cosmosDbContext = new Mock<ICosmosDbContext>(MockBehavior.Strict);
            _cosmosDbContext.Setup(x => x.HasUserAnExistingSession(_userId)).ReturnsAsync(true);

            var sut = new GetUserSessionQueryHandler(_cosmosDbContext.Object);

            var response = await sut.Handle(new GetUserSessionQuery()
            {
                UserId = _userId
            });

            response.Should().NotBeNull();
            response.UserHasExistingSession.Should().BeTrue();
        }
    }
}
