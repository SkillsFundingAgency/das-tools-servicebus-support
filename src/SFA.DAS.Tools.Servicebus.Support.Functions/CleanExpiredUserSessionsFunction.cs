using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;

namespace SFA.DAS.Tools.Servicebus.Support.Functions;

public class CleanExpiredUserSessionsFunction(
    IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse> expiredUserSessionQuery,
    IQueryHandler<GetUserSessionsQuery, GetUserSessionsQueryResponse> userSessionsQuery,
    IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
    IMessageService messageService,
    IOrphanedMessageRecoveryService orphanedMessageRecoveryService,
    ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse> deleteUserSessionCommand,
    IUserService userService,
    ILogger<CleanExpiredUserSessionsFunction> logger)
{
    [Function("CleanExpiredUserSessionsFunction")]
    public async Task Run([TimerTrigger("%CleanExpiredUserSessionsFunctionTimer%", RunOnStartup = false)] TimerInfo myTimer)
    {
        try
        {
            var queryResult = await expiredUserSessionQuery.Handle(new GetExpiredUserSessionsQuery());

            foreach (var session in queryResult.ExpiredUserSessions)
            {
                userService.Configure(session.UserId, "CleanExpiredUserSessionsFunction");

                var getMessagesResponse = await GetMessages(session.UserId);
                var iterations = 0;

                while (getMessagesResponse.Messages.Any())
                {
                    iterations++;
                    if (iterations > 200)
                    {
                        logger.LogError(
                            "Stopping expired session cleanup after max iterations for userId={UserId} queue={Queue}",
                            session.UserId, session.Queue);
                        break;
                    }

                    await messageService.AbortMessages(getMessagesResponse.Messages, session.Queue);
                    logger.LogInformation(
                        "Released expired-session messages userId={UserId} queue={Queue} batchCount={BatchCount} iteration={Iteration}",
                        session.UserId, session.Queue, getMessagesResponse.Messages.Count(), iterations);
                    getMessagesResponse = await GetMessages(session.UserId);
                }

                if (!getMessagesResponse.Messages.Any())
                {
                    await deleteUserSessionCommand.Handle(new DeleteUserSessionCommand
                    {
                        Id = session.Id,
                        UserId = session.UserId
                    });
                }
            }

            var activeSessions = (await userSessionsQuery.Handle(new GetUserSessionsQuery()))
                .UserSessions
                .Where(s => s.ExpiryDateUtc >= DateTime.UtcNow);
            await orphanedMessageRecoveryService.RecoverOrphanedMessagesAsync(activeSessions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CleanExpiredUserSessionsFunction failed");
        }
    }

    private async Task<GetMessagesQueryResponse> GetMessages(string userId)
    {
        return await getMessagesQuery.Handle(new GetMessagesQuery
        {
            UserId = userId,
            SearchProperties = new SearchProperties
            {
                Offset = 0,
                Limit = 100
            }
        });
    }
}