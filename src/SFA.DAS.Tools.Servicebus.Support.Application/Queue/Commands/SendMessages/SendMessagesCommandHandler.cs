using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services.ServiceBus;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessages
{
    public class SendMessagesCommandHandler : ICommandHandler<SendMessagesCommand, SendMessagesCommandResponse>
    {
        private readonly IAsbService _asbService;

        public SendMessagesCommandHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<SendMessagesCommandResponse> Handle(SendMessagesCommand query)
        {
            await _asbService.SendMessagesAsync(query.Messages, query.QueueName);

            return new SendMessagesCommandResponse();
        }
    }
}
