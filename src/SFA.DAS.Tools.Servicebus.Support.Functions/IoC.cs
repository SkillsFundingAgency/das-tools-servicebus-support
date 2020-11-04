using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;

namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton(s =>
            {
                var cosmosDbSettings = s.GetRequiredService<CosmosDbSettings>();
                return new CosmosClient(cosmosDbSettings.Url, cosmosDbSettings.AuthKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });

            services.AddTransient<IAsbService, AsbService>();
            services.AddTransient<ICosmosMessageDbContext, CosmosMessageDbContext>();
            services.AddTransient<ICosmosUserSessionDbContext, CosmosUserSessionDbContext>();
            services.AddSingleton<IUserService, FunctionUserService>();
            services.AddTransient<IBatchSendMessageStrategy, BatchSendMessageStrategy>();
            services.AddTransient<ICosmosInfrastructureService, CosmosInfrastructureService>();
            services.AddTransient<IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse>, GetExpiredUserSessionsQueryHandler>();
            services.AddTransient<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>, GetMessagesQueryHandler>();
            services.AddTransient<ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>, DeleteUserSessionCommandHandler>();
            services.AddTransient<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>, BulkCreateQueueMessagesCommandHandler>();
            services.AddTransient<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>, ReceiveQueueMessagesQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>, GetQueueMessageCountQueryHandler>();
            services.AddTransient<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>, SendMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>, DeleteQueueMessagesCommandHandler>();
            services.AddTransient<IMessageService, MessageService>();
            services.AddSingleton<ICosmosDbPolicies, CosmosDbPolicies>();
            services.AddSingleton<IServiceBusPolicies, ServiceBusPolicies>();

            return services;
        }
    }
}
