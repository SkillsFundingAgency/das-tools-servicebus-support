using Microsoft.Extensions.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Domain;
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
        private readonly IConfiguration _config;
        private UserSession CurrentUserSession = null;

        public UserSessionService(ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse> upsertUserSessionCommand,
            IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> getUserSessionQuery,
            ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> deleteUserSessionCommand,
            IUserService userService,
            IConfiguration config
            )
        {
            _upsertUserSessionCommand = upsertUserSessionCommand;
            _getUserSessionQuery = getUserSessionQuery;
            _deleteUserSessionCommand = deleteUserSessionCommand;
            _userService = userService;
            _config = config;
        }

        public async Task<UserSession> UpsertUserSession(string queue)
        {
            var userSession = CurrentUserSession != null ? CurrentUserSession : await GetUserSession();

            var result = await _upsertUserSessionCommand.Handle(new UpsertUserSessionCommand()
            {
                UserSession = new Domain.UserSession
                {
                    Id = userSession?.Id ?? Guid.NewGuid().ToString(),
                    UserId = _userService.GetUserId(),
                    UserName = _userService.GetName(),
                    ExpiryDateUtc = DateTime.UtcNow.AddHours(_config.GetValue<int>("UserSessionExpirtyHours")), 
                    Queue = queue
                }
            });

            CurrentUserSession = result.UserSession;

            return result.UserSession;
        }

        public async Task DeleteUserSession()
        {
            var userSession = CurrentUserSession != null ? CurrentUserSession : await GetUserSession();

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
    }
}
