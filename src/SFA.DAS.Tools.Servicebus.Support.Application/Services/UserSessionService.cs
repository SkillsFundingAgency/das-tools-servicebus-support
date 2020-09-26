using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.CreateUserSession;
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
        private readonly ICommandHandler<CreateUserSessionCommand, CreateUserSessionCommandResponse> _createUserSessionCommand;
        private readonly IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> _getUserSessionQuery;
        private readonly ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> _deleteUserSessionCommand;
        private readonly IUserService _userService;

        public UserSessionService(ICommandHandler<CreateUserSessionCommand, CreateUserSessionCommandResponse> createUserSessionCommand,
            IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse> getUserSessionQuery,
            ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> deleteUserSessionCommand,
            IUserService userService
            )
        {
            _createUserSessionCommand = createUserSessionCommand;
            _getUserSessionQuery = getUserSessionQuery;
            _deleteUserSessionCommand = deleteUserSessionCommand;
            _userService = userService;
        }

        public async Task<UserSession> CreateUserSession()
        {
            var result = await _createUserSessionCommand.Handle(new CreateUserSessionCommand()
            {
                UserSession = new Domain.UserSession
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = _userService.GetUserId(),
                    UserName = _userService.GetName()
                }
            });

            return result.UserSession;
        }

        public async Task DeleteUserSession()
        {
            var userSession = await GetUserSession();

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
