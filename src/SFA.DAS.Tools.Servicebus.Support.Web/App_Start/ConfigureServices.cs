using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Extensions;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;
using System.Linq;
using Polly.Registry;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using Microsoft.AspNetCore.Http;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Audit.Client;
using System.Configuration;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<PolicyRegistry>();

            services.AddScoped<IAsbService, AsbService>(s =>
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

            services.AddTransient<ICosmosInfrastructureService, CosmosInfrastructureService>(s => new CosmosInfrastructureService(configuration, s.GetRequiredService<CosmosClient>()));
            services.AddTransient<ICosmosMessageDbContext, CosmosMessageDbContext>(s => new CosmosMessageDbContext(s.GetService<IUserService>(), s.GetRequiredService<ILogger<CosmosMessageDbContext>>(), s.GetRequiredService<ICosmosInfrastructureService>()));
            services.AddTransient<ICosmosUserSessionDbContext, CosmosUserSessionDbContext>(s => new CosmosUserSessionDbContext(s.GetRequiredService<ICosmosInfrastructureService>()));

            services.AddSingleton(s =>
            {
                var cosmosEndpointUrl = configuration.GetValue<string>("CosmosDb:Url");
                var cosmosAuthenticationKey = configuration.GetValue<string>("CosmosDb:AuthKey");

                return new CosmosClient(cosmosEndpointUrl, cosmosAuthenticationKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });

            services.AddTransient<IUserService, UserService>(s=> new UserService(s.GetRequiredService<IHttpContextAccessor>(), configuration.GetValue<string>("NameClaim")));
            services.AddTransient<IBatchGetMessageStrategy, BatchGetMessageStrategy>();
            services.AddTransient<IBatchSendMessageStrategy, BatchSendMessageStrategy>();

            services.AddTransient<IMessageService, MessageService>(s => new MessageService(
                s.GetService<IBatchSendMessageStrategy>(),
                s.GetRequiredService<ILogger<MessageService>>(),
                s.GetService<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>>(),
                s.GetService<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>>(), 
                s.GetService<IAuditService>()
            ));

            services.AddTransient<IRetrieveMessagesService, RetrieveMessagesService>(s =>
                {
                    var serviceBusConnectionString = configuration.GetValue<string>("ServiceBusRepoSettings:ServiceBusConnectionString");
                    var connectionBuilder = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
                    var tokenProvider = TokenProvider.CreateManagedIdentityTokenProvider();

                    return new RetrieveMessagesService(
                        s.GetRequiredService<ILogger<RetrieveMessagesService>>(),
                        configuration.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize"),
                        s.GetService<IBatchGetMessageStrategy>(),
                        s.GetService<IUserService>(),
                        s.GetService<ICosmosMessageDbContext>(),
                        configuration.GetValue<int>("ServiceBusRepoSettings:MaxRetrievalSize"),
                        new MessageReceiverFactory(connectionBuilder, tokenProvider)
                    );
                }
            );

            services.AddSingleton<IMessageDetailRedactor, MessageDetailRedactor>(s => new MessageDetailRedactor(configuration.GetSection("RedactPatterns").GetChildren().AsEnumerable().Select(a => a.Value)));

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
            services.AddTransient<KeepUserSessionActiveFilter>(s => new KeepUserSessionActiveFilter(s.GetRequiredService<IUserSessionService>(), configuration));

            services.AddTransient<IAuditApiConfiguration>(s => new AuditApiConfiguration {
                ApiBaseUrl = configuration.GetValue<string>("ApiBaseUrl"),
                ClientId = configuration.GetValue<string>("ClientId"),
                ClientSecret = configuration.GetValue<string>("ClientSecret"),
                IdentifierUri = configuration.GetValue<string>("IdentifierUri"),
                Tenant = configuration.GetValue<string>("Tenant")
            });
            services.AddTransient<IAuditApiClient, AuditApiClient>();
            services.AddTransient<IAuditMessageFactory, AuditMessageFactory>();
            services.AddTransient<IAuditService, AuditService>();

            return services;
        }

        private static ManagementClient CreateManagementClient(ServiceBusConnectionStringBuilder connectionBuilder, ITokenProvider tokenProvider) => connectionBuilder.HasSasKey()
            ? new ManagementClient(connectionBuilder)
            : new ManagementClient(connectionBuilder, tokenProvider);
    }
}
