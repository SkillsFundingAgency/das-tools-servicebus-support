using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using System.Linq;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {

            services.AddTransient<IAsbService, AsbService>(s =>
            {
                var serviceBusConnectionString = configuration.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
                var connectionBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
                var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();

                return new AsbService(s.GetService<IUserService>(),
                    configuration,
                    s.GetRequiredService<ILogger<AsbService>>(),
                    tokenProvider,
                    connectionBuilder,
                    CreateManagementClient(connectionBuilder, tokenProvider),
                    new BatchMessageStrategy()
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

            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>, GetMessagesQueryHandler>();
            services.AddTransient<IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse>, GetUserSessionQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse>, GetQueuesQueryHandler>();
            services.AddTransient<IQueryHandler<PeekQueueMessagesQuery, PeekQueueMessagesQueryResponse>, PeekQueueMessagesQueryHandler>();
            services.AddTransient<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>, BulkCreateQueueMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>, SendMessagesCommandHandler>();
            services.AddTransient<IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse>, GetQueueDetailsQueryHandler>();
            services.AddTransient<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>, ReceiveQueueMessagesQueryHandler>();
            services.AddTransient<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>, DeleteQueueMessagesCommandHandler>();
            services.AddTransient<IQueryHandler<GetMessageQuery, GetMessageQueryResponse>, GetMessageQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>, GetQueueMessageCountQueryHandler>();
            services.AddTransient<IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse>, GetMessagesByIdQueryHandler>();
            services.AddTransient<ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse>, UpsertUserSessionCommandHandler>();
            services.AddTransient<ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>, DeleteUserSessionCommandHandler>();
            services.AddTransient<IBatchMessageStrategy, BatchMessageStrategy>();
            services.AddTransient<IUserSessionService, UserSessionService>(s =>
                new UserSessionService(
                    s.GetService<ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse>>(),
                    s.GetService<IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse>>(),
                    s.GetService<ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>>(),
                    s.GetService<IUserService>(),
                    configuration,
                    s.GetService<IHttpContextAccessor>()
                )
            );
            services.AddTransient<IMessageService, MessageService>(s =>
                 new MessageService(
                    s.GetService<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>>(),
                    s.GetService<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>>(),
                    s.GetService<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>>(),
                    s.GetService<IBatchMessageStrategy>(),
                    s.GetRequiredService<ILogger<MessageService>>(),
                    configuration.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize"),
                    s.GetService<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>(),
                    s.GetService<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>()
                )
            );

            services.AddSingleton<IMessageDetailRedactor, MessageDetailRedactor>(s => new MessageDetailRedactor(configuration.GetSection("RedactPatterns").GetChildren().AsEnumerable().Select(a => a.Value)));
            services.AddTransient<KeepUserSessionActiveFilter>(s => new KeepUserSessionActiveFilter(s.GetRequiredService<IUserSessionService>(), configuration));


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
