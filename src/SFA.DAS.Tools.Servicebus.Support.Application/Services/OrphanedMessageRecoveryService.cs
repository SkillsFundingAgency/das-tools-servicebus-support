using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Services;

public class OrphanedMessageRecoveryService(
    ICosmosMessageDbContext cosmosMessageDbContext,
    IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse> getMessagesQuery,
    IMessageService messageService,
    IUserService userService,
    ILogger<OrphanedMessageRecoveryService> logger)
    : IOrphanedMessageRecoveryService
{
    public async Task RecoverOrphanedMessagesAsync(IEnumerable<UserSession> activeSessions, int maxIterations = 200)
    {
        var activeUserIds = new HashSet<string>(
            activeSessions.Where(s => !string.IsNullOrWhiteSpace(s.UserId)).Select(s => s.UserId));

        var orphanedOwners = (await cosmosMessageDbContext.GetDistinctMessageOwnersAsync())
            .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
            .Where(x => !string.IsNullOrWhiteSpace(x.Queue))
            .Where(x => !activeUserIds.Contains(x.UserId))
            .DistinctBy(x => $"{x.UserId}:{x.Queue}")
            .ToList();

        foreach (var owner in orphanedOwners)
        {
            userService.Configure(owner.UserId, "CleanExpiredUserSessionsFunction-OrphanedRecovery");

            var iterations = 0;
            var getMessagesResponse = await GetMessages(owner.UserId);

            while (getMessagesResponse.Messages.Any())
            {
                iterations++;
                if (iterations > maxIterations)
                {
                    logger.LogError(
                        "Stopping orphan recovery after max iterations for userId={UserId} queue={Queue} maxIterations={MaxIterations}",
                        owner.UserId, owner.Queue, maxIterations);
                    break;
                }

                await messageService.AbortMessages(getMessagesResponse.Messages, owner.Queue);
                logger.LogInformation(
                    "Recovered orphaned messages batch userId={UserId} queue={Queue} batchCount={BatchCount} iteration={Iteration}",
                    owner.UserId, owner.Queue, getMessagesResponse.Messages.Count(), iterations);
                getMessagesResponse = await GetMessages(owner.UserId);
            }
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