using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessageToErrorQueue;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.SvcBusService;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class IoC
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IAsbService, AsbService>(s => new AsbService(s.GetService<IUserService>(), configuration, s.GetRequiredService<ILogger<AsbService>>()));            
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
            services.AddTransient<ICommandHandler<SendMessageToErrorQueueCommand, SendMessageToErrorQueueCommandResponse>, SendMessageToErrorQueueCommandHandler>();
            services.AddTransient<IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse>, GetQueueDetailsQueryHandler>();
            services.AddTransient<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>, ReceiveQueueMessagesQueryHandler>();
            services.AddTransient<ICommandHandler<DeleteQueueMessageCommand, DeleteQueueMessageCommandResponse>, DeleteQueueMessageCommandHandler>();
            services.AddTransient<IQueryHandler<GetMessageQuery, GetMessageQueryResponse>, GetMessageQueryHandler>();

            return services;
        }
    }
}
