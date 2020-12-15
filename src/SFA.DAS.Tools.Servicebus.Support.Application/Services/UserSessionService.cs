using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services
{
    public class UserSessionService : IUserSessionService
    {
        private readonly ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse> _upsertUserSessionCommand;
        private readonly IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> _getUserSessionQuery;
        private readonly ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> _deleteUserSessionCommand;
        private readonly IUserService _userService;
        private readonly int _userSessionExpiryHours;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserSessionService(ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse> upsertUserSessionCommand,
            IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> getUserSessionQuery,
            ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> deleteUserSessionCommand,
            IUserService userService,
            UserIdentitySettings config,
            IHttpContextAccessor httpContextAccessor
            )
        {
            _upsertUserSessionCommand = upsertUserSessionCommand;
            _getUserSessionQuery = getUserSessionQuery;
            _deleteUserSessionCommand = deleteUserSessionCommand;
            _userService = userService;
            _userSessionExpiryHours = config.UserSessionExpiryHours <= 0 ? 24 : config.UserSessionExpiryHours;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<UserSession> UpsertUserSession(string queue)
        {
            var userSession = await GetUserSession();

            if (userSession != null)
            {
                // Update Expiry
                userSession.ExpiryDateUtc = DateTime.UtcNow.AddHours(_userSessionExpiryHours);
            }
            else
            {
                // Create new session
                userSession = CreateUserSession(queue);
            }

            var result = await _upsertUserSessionCommand.Handle(new UpsertUserSessionCommand()
            {
                UserSession = userSession
            });

            return result.UserSession;
        }

        public async Task DeleteUserSession()
        {
            var currentSession = _httpContextAccessor.HttpContext.Session.Get<UserSession>("userSession");
            var userSession = currentSession ?? await GetUserSession();

            if (userSession != null)
            {
                await _deleteUserSessionCommand.Handle(new DeleteUserSessionCommand()
                {
                    Id = userSession.Id,
                    UserId = _userService.GetUserId()
                });
            }
        }

        public async Task<UserSession> GetUserSession()
        {
            var result = await _getUserSessionQuery.Handle(new GetUserSessionQuery()
            {
                UserId = _userService.GetUserId()
            });

            return result.UserSession;
        }

        private UserSession CreateUserSession(string queueName)
        {
            return new UserSession
            {
                Id = Guid.NewGuid().ToString(),
                UserId = _userService.GetUserId(),
                UserName = _userService.GetName(),
                ExpiryDateUtc = DateTime.UtcNow.AddHours(_userSessionExpiryHours),
                Queue = queueName
            };
        }

    }
}
