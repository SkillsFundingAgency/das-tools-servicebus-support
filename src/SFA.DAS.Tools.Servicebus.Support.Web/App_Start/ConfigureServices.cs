using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Tools.Servicebus.Support.Application.Services;
using SFA.DAS.Tools.Servicebus.Support.Domain.Configuration;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddSingleton(s =>
            {
                var cosmosDbSettings = s.GetRequiredService<CosmosDbSettings>();
                return new CosmosClient(cosmosDbSettings.Url, cosmosDbSettings.AuthKey, new CosmosClientOptions() { AllowBulkExecution = true });
            });

            services.AddScoped<IAsbService, AsbService>();
            services.AddTransient<ICosmosInfrastructureService, CosmosInfrastructureService>();
            services.AddTransient<ICosmosMessageDbContext, CosmosMessageDbContext>();
            services.AddTransient<ICosmosUserSessionDbContext, CosmosUserSessionDbContext>();
            services.AddTransient<IUserService, UserService>();
            services.AddTransient<IBatchGetMessageStrategy, BatchGetMessageStrategy>();
            services.AddTransient<IBatchSendMessageStrategy, BatchSendMessageStrategy>();
            services.AddTransient<IMessageService, MessageService>();
            services.AddTransient<IMessageReceiverFactory, MessageReceiverFactory>();
            services.AddTransient<IRetrieveMessagesService, RetrieveMessagesService>();
            services.AddSingleton<IMessageDetailRedactor, MessageDetailRedactor>();
            services.AddTransient<IUserSessionService, UserSessionService>();
            services.AddTransient<KeepUserSessionActiveFilter>();
            services.AddSingleton<ICosmosDbPolicies, CosmosDbPolicies>();
            services.AddSingleton<IServiceBusPolicies, ServiceBusPolicies>();

            return services;
        }
    }
}
