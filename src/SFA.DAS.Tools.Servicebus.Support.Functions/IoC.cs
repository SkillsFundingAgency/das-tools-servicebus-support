using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;

namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ServiceBusErrorManagementSettings>(configuration.GetSection(ServiceBusErrorManagementSettings.ServiceBusErrorManagementSettingsKey));
            services.Configure<UserIdentitySettings>(configuration.GetSection(UserIdentitySettings.UserIdentitySettingsKey));
            services.Configure<CosmosDbSettings>(configuration.GetSection(CosmosDbSettings.CosmosDbSettingsKey));

            // Remove IOptions Abstraction from Configure
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<CosmosDbSettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<UserIdentitySettings>>().Value);
            services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<ServiceBusErrorManagementSettings>>().Value);

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
            services.AddTransient<IMessageService, MessageService>(s =>
                 new MessageService(
                    s.GetService<IBatchSendMessageStrategy>(),
                    s.GetRequiredService<ILogger<MessageService>>(),
                    s.GetService<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>(),
                    s.GetService<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>(),
                    s.GetService<IAuditService>()
                )
            );

            return services;
        }
    }
}
