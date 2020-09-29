using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetExpiredUserSessions;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Functions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;

[assembly: FunctionsStartup(typeof(Startup))]
namespace SFA.DAS.Tools.Servicebus.Support.Functions
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var sp = builder.Services.BuildServiceProvider();

            var executionContextOptions = builder.Services.BuildServiceProvider()
                .GetService<IOptions<ExecutionContextOptions>>()
                .Value
            ;
            var appDirectory = executionContextOptions.AppDirectory;
            var configurationService = sp.GetService<IConfiguration>();
            var configuration = LoadConfiguration(appDirectory, configurationService);


            builder.Services.AddTransient<IAsbService, AsbService>(s =>
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

            builder.Services.AddTransient<ICosmosDbContext, CosmosDbContext>(s => new CosmosDbContext(s.GetRequiredService<CosmosClient>(), s.GetService<IUserService>(), configuration, s.GetRequiredService<ILogger<CosmosDbContext>>()));
            builder.Services.AddSingleton(s =>
            {
                var cosmosEndpointUrl = configuration.GetValue<string>("CosmosDb:Url");
                var cosmosAuthenticationKey = configuration.GetValue<string>("CosmosDb:AuthKey");

                return new CosmosClient(cosmosEndpointUrl, cosmosAuthenticationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });
            builder.Services.AddTransient<IQueryHandler<GetExpiredUserSessionsQuery, GetExpiredUserSessionsQueryResponse>, GetExpiredUserSessionsQueryHandler>();
            builder.Services.AddTransient<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>, GetMessagesQueryHandler>();
            builder.Services.AddTransient<ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>, DeleteUserSessionCommandHandler>();
            builder.Services.AddTransient<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>, BulkCreateQueueMessagesCommandHandler>();
            builder.Services.AddTransient<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>, ReceiveQueueMessagesQueryHandler>();
            builder.Services.AddTransient<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>, GetQueueMessageCountQueryHandler>();
            builder.Services.AddTransient<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>, SendMessagesCommandHandler>();
            builder.Services.AddTransient<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>, DeleteQueueMessagesCommandHandler>();
            builder.Services.AddTransient<IMessageService, MessageService>(s =>
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
        }

        private IConfiguration LoadConfiguration(string appDirectory, IConfiguration configurationService)
        {
            var builder = new ConfigurationBuilder()
               .SetBasePath(appDirectory)
               .AddJsonFile("local.settings.json",
                   optional: true,
                   reloadOnChange: true)
               .AddConfiguration(configurationService);

            return builder.Build();
        }

        private ManagementClient CreateManagementClient(ServiceBusConnectionStringBuilder connectionBuilder, TokenProvider tokenProvider)
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
