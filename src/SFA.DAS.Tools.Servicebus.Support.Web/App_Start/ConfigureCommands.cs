using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BatchDeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.BulkCreateQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.DeleteUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.UpsertUserSession;
using SFA.DAS.Tools.Servicebus.Support.Audit;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.CosmosDb;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class ConfigureCommands
    {
        public static IServiceCollection AddCommands(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<ICommandHandler<BulkCreateQueueMessagesCommand, BulkCreateQueueMessagesCommandResponse>, BulkCreateQueueMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>, SendMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<DeleteQueueMessagesCommand, DeleteQueueMessagesCommandResponse>, DeleteQueueMessagesCommandHandler>();
            services.AddTransient<ICommandHandler<BatchDeleteQueueMessagesCommand, BatchDeleteQueueMessagesCommandResponse>, BatchDeleteQueueMessagesCommandHandler>(
                s => new BatchDeleteQueueMessagesCommandHandler(
                    s.GetService<ICosmosMessageDbContext>(),
                    configuration.GetValue<int>("ServiceBusRepoSettings:PeekMessageBatchSize"),
                    s.GetService<ILogger<BatchDeleteQueueMessagesCommandHandler>>(),
                    s.GetService<IAuditService>()
                ));

            services.AddTransient<ICommandHandler<UpsertUserSessionCommand, UpsertUserSessionCommandResponse>, UpsertUserSessionCommandHandler>();
            services.AddTransient<ICommandHandler<DeleteUserSessionCommand, DeleteUserSessionCommandResponse>, DeleteUserSessionCommandHandler>();

            return services;
        }
    }
}
