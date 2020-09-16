using System.Runtime.InteropServices.ComTypes;
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
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
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
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;

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
                    new ManagementClient(connectionBuilder, tokenProvider),
                    new BatchMessageStrategy()
                );
            });

            services.AddTransient<ICosmosDbContext, CosmosDbContext>(s => new CosmosDbContext(s.GetRequiredService<CosmosClient>(), s.GetService<IUserService>(), configuration, s.GetRequiredService<ILogger<CosmosDbContext>>()));

            services.AddSingleton(s =>
            {
                var cosmosEndpointUrl = configuration.GetValue<string>("CosmosDb:Url");
                var cosmosAuthenticationKey = configuration.GetValue<string>("CosmosDb:AuthKey");
                
                return new CosmosClient(cosmosEndpointUrl, cosmosAuthenticationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });

            services.AddSingleton<IUserService, UserService>();
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

            services.AddTransient<IBatchMessageStrategy, BatchMessageStrategy>();
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

            return services;
        }
    }
}
