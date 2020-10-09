using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using NUnit.Framework;
using SFA.DAS.Tools.Servicebus.Support.Domain.Queue;
using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.Batching;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.UnitTests.Services.MessageService
{
    public class WhenBatchSendingMessages
    {
        [Test]
        public async Task ThenMessagesAreBatchedAccordingToMessageSize()
        {
            var numMessages = 1000;
            var messages = new List<QueueMessage>();
            var batchCount = 40;

            for (var i = 0; i < numMessages; i++)
            {
                var msg = new Message(Encoding.UTF8.GetBytes(
                    "{\"SystemId\":\"FAA\",\"TemplateId\":\"VacancyService_SendMobileVerificationCode\",\"RecipientsNumber\":\"1179250172\",\"Tokens\":{\"MobileVerificationCode\":\"1234\",\"CandidateFirstName\":\"jEfqr\",\"CandidateSiteDomainName\":\"at.findapprenticeship.service.gov.uk\"}}"));
                msg.UserProperties.Add("NServiceBus.Transport.Encoding", "application/octect-stream");
                msg.UserProperties.Add("NServiceBus.MessageId", "6d3ec025-71de-4e19-bdea-ab6d00d36594");
                msg.UserProperties.Add("NServiceBus.MessageIntent", "Send");
                msg.UserProperties.Add("NServiceBus.ConversationId", "63f9ab94-a10e-49bf-90d2-ab6d00d36594");
                msg.UserProperties.Add("NServiceBus.CorrelationId", "6d3ec025-71de-4e19-bdea-ab6d00d36594");
                msg.UserProperties.Add("NServiceBus.OriginatingMachine", "RD0003FF55A44E");
                msg.UserProperties.Add("NServiceBus.OriginatingEndpoint", "SFA.DAS.Notifications.Api");
                msg.UserProperties.Add("$.diagnostics.originating.hostid", "f7495f777ccdc253b8d766b6342b2245");
                msg.UserProperties.Add("NServiceBus.ContentType", "application/json");
                msg.UserProperties.Add("NServiceBus.EnclosedMessageTypes", "SFA.DAS.Notifications.Messages.Commands.SendSmsCommand, SFA.DAS.Notifications.Messages, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null");
                msg.UserProperties.Add("NServiceBus.Version", "7.2.0");
                msg.UserProperties.Add("NServiceBus.TimeSent", "2020-02-26 12:49:40:329484 Z");
                msg.UserProperties.Add("Diagnostic-Id", "00-a89ce968912ec44dbc1dbac5801fea0a-5759f16ba52a984a-00");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.ExceptionType", "Notify.Exceptions.NotifyClientException");
                msg.UserProperties.Add("NServiceBus.Retries.Timestamp", "2020-02-26 12:50:20:841718 Z");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.HelpLink", string.Empty);
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Message", "Status code 400. Error: {\"errors\":[{\"error\":\"BadRequestError\",\"message\":\"Cannot send to international mobile numbers\"}],\"status_code\":400}\n, Exception: Status code 400. The following errors occured [\r\n  {\r\n    \"error\": \"BadRequestError\",\r\n    \"message\": \"Cannot send to international mobile numbers\"\r\n  }\r\n]");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Source", "Notify");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.StackTrace", "Notify.Exceptions.NotifyClientException: Status code 400. Error: {\"errors\":[{\"error\":\"BadRequestError\",\"message\":\"Cannot send to international mobile numbers\"}],\"status_code\":400}\n, Exception: Status code 400. The following errors occured [\r\n  {\r\n    \"error\": \"BadRequestError\",\r\n    \"message\": \"Cannot send to international mobile numbers\"\r\n  }\r\n]\r\n   at SFA.DAS.Notifications.Infrastructure.ExecutionPolicies.ExecutionPolicy.OnException(Exception ex) in /vsts/agent/_work/1/s/src/SFA.DAS.Notifications.Infrastructure/ExecutionPolicies/ExecutionPolicy.cs:line 60\r\n   at SFA.DAS.Notifications.Infrastructure.ExecutionPolicies.ExecutionPolicy.ExecuteAsync(Func`1 action) in /vsts/agent/_work/1/s/src/SFA.DAS.Notifications.Infrastructure/ExecutionPolicies/ExecutionPolicy.cs:line 30\r\n   at SFA.DAS.Notifications.Application.Commands.SendSms.SendSmsMediatRCommandHandler.Handle(SendSmsMediatRCommand command, CancellationToken cancellationToken) in /vsts/agent/_work/1/s/src/SFA.DAS.Notifications.Application/Commands/SendSms/SendSmsMediatRCommandHandler.cs:line 59\r\n   at MediatR.AsyncRequestHandler`1.MediatR.IRequestHandler<TRequest,MediatR.Unit>.Handle(TRequest request, CancellationToken cancellationToken)\r\n   at SFA.DAS.Notifications.MessageHandlers.CommandHandlers.SendSmsCommandHandler.Handle(SendSmsCommand message, IMessageHandlerContext context) in /vsts/agent/_work/1/s/src/SFA.DAS.Notifications.MessageHandlers/CommandHandlers/SendSmsCommandHandler.cs:line 33\r\n   at NServiceBus.InvokeHandlerTerminator.Terminate(IInvokeHandlerContext context)\r\n   at SFA.DAS.UnitOfWork.NServiceBus.Behaviors.UnitOfWorkContextBehavior.Invoke(IInvokeHandlerContext context, Func`1 next)\r\n   at NServiceBus.LoadHandlersConnector.Invoke(IIncomingLogicalMessageContext context, Func`2 stage)\r\n   at NServiceBus.ScheduledTaskHandlingBehavior.Invoke(IIncomingLogicalMessageContext context, Func`2 next)\r\n   at SFA.DAS.UnitOfWork.NServiceBus.Behaviors.UnitOfWorkBehavior.Invoke(IIncomingLogicalMessageContext context, Func`1 next)\r\n   at NServiceBus.DeserializeMessageConnector.Invoke(IIncomingPhysicalMessageContext context, Func`2 stage)\r\n   at RegisterCurrentSessionBehavior.Invoke(IIncomingPhysicalMessageContext context, Func`2 next)\r\n   at NServiceBus.Transport.AzureServiceBus.TransactionScopeSuppressBehavior.Invoke(IIncomingPhysicalMessageContext context, Func`1 next)\r\n   at NServiceBus.ProcessingStatisticsBehavior.Invoke(IIncomingPhysicalMessageContext context, Func`2 next)\r\n   at NServiceBus.TransportReceiveToPhysicalMessageConnector.Invoke(ITransportReceiveContext context, Func`2 next)\r\n   at NServiceBus.MainPipelineExecutor.Invoke(MessageContext messageContext)\r\n   at NServiceBus.Transport.AzureServiceBus.MessagePump.ProcessMessage(Task`1 receiveTask)");
                msg.UserProperties.Add("NServiceBus.TimeOfFailure", "2020-04-22 15:39:59:130839 Z");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Data.Message type", "SFA.DAS.Notifications.Messages.Commands.SendSmsCommand");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Data.Handler type", "SFA.DAS.Notifications.MessageHandlers.CommandHandlers.SendSmsCommandHandler");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Data.Handler start time", "4/22/2020 3:39:58 PM");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Data.Handler failure time", "4/22/2020 3:39:59 PM");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Data.Message ID", "6d3ec025-71de-4e19-bdea-ab6d00d36594");
                msg.UserProperties.Add("NServiceBus.ExceptionInfo.Data.Transport message ID", "750f2668-7456-42c2-abba-c9d481d6b0ad");
                msg.UserProperties.Add("NServiceBus.FailedQ", "SFA.DAS.Notifications.MessageHandlers");
                msg.UserProperties.Add("NServiceBus.ProcessingMachine", "RD0003FF1F9DA3");
                msg.UserProperties.Add("NServiceBus.ProcessingEndpoint", "SFA.DAS.Notifications.MessageHandlers");
                msg.UserProperties.Add("$.diagnostics.hostid", "467a1cad2adb04ba2eeb723e39dff0d7");
                msg.UserProperties.Add("$.diagnostics.hostdisplayname", "RD0003FF1F9DA3");

                messages.Add(new QueueMessage()
                {
                    OriginalMessage = msg
                });
            }

            var sut = new BatchSendMessageStrategy();
            var count = 0;

            await sut.Execute(messages, async (messages) =>
            {
                count++;
            });

            count.Should().Be(batchCount);
        }
    }
}
