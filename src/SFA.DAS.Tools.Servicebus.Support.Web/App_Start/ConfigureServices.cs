using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
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
                    CreateManagementClient(connectionBuilder, tokenProvider)
                );
            });

            services.AddTransient<ICosmosInfrastructureService, CosmosInfrastructureService>();
            services.AddTransient<ICosmosMessageDbContext, CosmosMessageDbContext>(s => new CosmosMessageDbContext(s.GetRequiredService<CosmosClient>(), s.GetService<IUserService>(), configuration, s.GetRequiredService<ILogger<CosmosMessageDbContext>>(), s.GetRequiredService<ICosmosInfrastructureService>()));
            services.AddTransient<ICosmosUserSessionDbContext, CosmosUserSessionDbContext>(s => new CosmosUserSessionDbContext(s.GetRequiredService<CosmosClient>(), s.GetRequiredService<ICosmosInfrastructureService>(), configuration));

            services.AddSingleton(s =>
            {
                var cosmosEndpointUrl = configuration.GetValue<string>("CosmosDb:Url");
                var cosmosAuthenticationKey = configuration.GetValue<string>("CosmosDb:AuthKey");

                return new CosmosClient(cosmosEndpointUrl, cosmosAuthenticationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });

            services.AddTransient<IUserService, UserService>();            
            services.AddTransient<IBatchGetMessageStrategy, BatchGetMessageStrategy>();
            services.AddTransient<IBatchSendMessageStrategy, BatchSendMessageStrategy>();
            
            services.AddTransient<IMessageService, MessageService>(s =>
                 new MessageService(
                    s.GetService<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>>(),
                    s.GetService<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>>(),
                    s.GetService<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>>(),
                    s.GetService<IBatchGetMessageStrategy>(),
                    s.GetService<IBatchSendMessageStrategy>(),
                    s.GetRequiredService<ILogger<MessageService>>(),
                    configuration.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize"),
                    s.GetService<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>(),
                    s.GetService<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>()
                )
            );

            services.AddSingleton<IMessageDetailRedactor, MessageDetailRedactor>(s => new MessageDetailRedactor(configuration.GetSection("RedactPatterns").GetChildren().AsEnumerable().Select(a => a.Value)));
            services.AddTransient<KeepUserSessionActiveFilter>();


            return services;
        }

        private static ManagementClient CreateManagementClient(ServiceBusConnectionStringBuilder connectionBuilder, ITokenProvider tokenProvider)
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
