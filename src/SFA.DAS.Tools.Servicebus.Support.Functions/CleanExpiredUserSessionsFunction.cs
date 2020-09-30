using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public class CleanExpiredUserSessionsFunction
    {
        private readonly IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse> _expiredUserSessionQuery;
        private readonly IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> _getMessagesQuery;
        private readonly IMessageService _messageService;
        private readonly ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> _deleteUserSessionCommand;
        private readonly IUserService _userService;

        public CleanExpiredUserSessionsFunction(
            IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse> expiredUserSessionQuery,
            IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
            IMessageService messageService, 
            ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> deleteUserSessionCommand, 
            IUserService userService
            )
        {
            _expiredUserSessionQuery = expiredUserSessionQuery;
            _getMessagesQuery = getMessagesQuery;
            _messageService = messageService;
            _deleteUserSessionCommand = deleteUserSessionCommand;
            _userService = userService;
        }

        [FunctionName("CleanExpiredUserSessionsFunction")]
        public async Task Run([TimerTrigger("%CleanExpiredUserSessionsFunctionTimer%")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                var queryResult = await _expiredUserSessionQuery.Handle(new GetExpiredUserSessionsQuery());

                foreach (var session in queryResult.ExpiredUserSessions)
                {
                    _userService.Configure(session.UserId, "CleanExpiredUserSessionsFunction");
                    while (true)
                    {
                        var getMessagesResponse = await _getMessagesQuery.Handle(new GetMessagesQuery()
                        {
                            UserId = session.UserId,
                            SearchProperties = new SearchProperties
                            {
                                Offset = 0                             
                            }
                        });

                        if (getMessagesResponse?.Count > 0)
                        {
                            await _messageService.AbortMessages(getMessagesResponse.Messages, session.Queue);
                        }
                        else
                        {
                            await _deleteUserSessionCommand.Handle(new DeleteUserSessionCommand()
                            {
                                Id = session.Id,
                                UserId = session.UserId
                            });
                            break;
                        }
                    }
                }
            }catch(Exception ex)
            {
                log.LogError(ex, "CleanExpiredUserSessionsFunction");
            }
        }
    }
}
