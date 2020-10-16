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
            services.AddSingleton<PolicyRegistry>();

            services.AddTransient<IAsbService, AsbService>(s =>
            {
                var policyRegistry = s.GetRequiredService<PolicyRegistry>();
                policyRegistry.ConfigureWaitAndRetry(Constants.MessageQueueWaitAndRetry, s.GetRequiredService<ILogger<AsbService>>());
                var serviceBusConnectionString = configuration.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
                var connectionBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
                var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();

                return new AsbService(s.GetService<IUserService>(),
                    configuration,
                    s.GetRequiredService<ILogger<AsbService>>(),
                    tokenProvider,
                    connectionBuilder,
                    CreateManagementClient(connectionBuilder, tokenProvider),
                    policyRegistry.Get<IAsyncPolicy>(Constants.MessageQueueWaitAndRetry)
                );
            });
            services.AddTransient<ICosmosInfrastructureService, CosmosInfrastructureService>(s => new CosmosInfrastructureService(configuration));
            services.AddTransient<ICosmosMessageDbContext, CosmosMessageDbContext>(s => new CosmosMessageDbContext(s.GetRequiredService<CosmosClient>(), s.GetService<IUserService>(), configuration, s.GetRequiredService<ILogger<CosmosMessageDbContext>>(), s.GetRequiredService<ICosmosInfrastructureService>()));
            services.AddTransient<ICosmosUserSessionDbContext, CosmosUserSessionDbContext>(s => new CosmosUserSessionDbContext(s.GetRequiredService<CosmosClient>(), s.GetRequiredService<ICosmosInfrastructureService>(), configuration));
            services.AddSingleton(s =>
            {
                var cosmosEndpointUrl = configuration.GetValue<string>("CosmosDb:Url");
                var cosmosAuthenticationKey = configuration.GetValue<string>("CosmosDb:AuthKey");

                return new CosmosClient(cosmosEndpointUrl, cosmosAuthenticationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });
            services.AddSingleton<IUserService, FunctionUserService>();
            services.AddTransient<IBatchSendMessageStrategy, BatchSendMessageStrategy>();
            services.AddTransient<ICosmosInfrastructureService, CosmosInfrastructureService>(s=> new CosmosInfrastructureService(configuration));
            services.AddTransient<IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse>, GetExpiredUserSessionsQueryHandler>();
            services.AddTransient<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>, GetMessagesQueryHandler>();
            services.AddTransient<ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>, DeleteUserSessionCommandHandler>();
            services.AddTransient<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>, BulkCreateQueueMessagesCommandHandler>();
            services.AddTransient<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>, ReceiveQueueMessagesQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>, GetQueueMessageCountQueryHandler>();
            services.AddTransient<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>, SendMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>, DeleteQueueMessagesCommandHandler>();
            services.AddTransient<IMessageService, MessageService>(s =>
                 new MessageService(
                    s.GetService<IBatchSendMessageStrategy>(),
                    s.GetRequiredService<ILogger<MessageService>>(),
                    s.GetService<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>(),
                    s.GetService<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>()
                )
            );

            return services;
        }

        private static ManagementClient CreateManagementClient(ServiceBusConnectionStringBuilder connectionBuilder, TokenProvider tokenProvider)
        {
            if (connectionBuilder.SasKey?.Length > 0)
            {
                return new ManagementClient(connectionBuilder);
            }
            else
            {
                return new ManagementClient(connectionBuilder, tokenProvider);
            }
        }
    }
}
