using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SFA.DAS.Tools.Servicebus.Support.Application;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessage;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessageCountPerUser;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetMessagesById;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueDetails;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueueMessageCount;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetQueues;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.GetUserSession;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.PeekQueueMessages;
using SFA.DAS.Tools.Servicebus.Support.Application.Queue.Queries.ReceiveQueueMessages;

namespace SFA.DAS.Tools.Servicebus.Support.Web.App_Start
{
    public static class ConfigureQueries
    {
        public static IServiceCollection AddQueries(this IServiceCollection services)
        {
            services.AddTransient<IQueryHandler<GetMessagesQuery, GetMessagesQueryResponse>, GetMessagesQueryHandler>();
            services.AddTransient<IQueryHandler<GetUserSessionQuery, GetUserSessionQueryResponse>, GetUserSessionQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueuesQuery, GetQueuesQueryResponse>, GetQueuesQueryHandler>();
            services.AddTransient<IQueryHandler<PeekQueueMessagesQuery, PeekQueueMessagesQueryResponse>, PeekQueueMessagesQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueueDetailsQuery, GetQueueDetailsQueryResponse>, GetQueueDetailsQueryHandler>();
            services.AddTransient<IQueryHandler<ReceiveQueueMessagesQuery, ReceiveQueueMessagesQueryResponse>, ReceiveQueueMessagesQueryHandler>();
            services.AddTransient<IQueryHandler<GetMessageQuery, GetMessageQueryResponse>, GetMessageQueryHandler>();
            services.AddTransient<IQueryHandler<GetQueueMessageCountQuery, GetQueueMessageCountQueryResponse>, GetQueueMessageCountQueryHandler>();
            services.AddTransient<IQueryHandler<GetMessagesByIdQuery, GetMessagesByIdQueryResponse>, GetMessagesByIdQueryHandler>();
            services.AddTransient<IQueryHandler<GetMessageCountPerUserQuery, GetMessageCountPerUserQueryResponse>, GetMessageCountPerUserQueryHandler>();

            return services;
        }
    }
}
