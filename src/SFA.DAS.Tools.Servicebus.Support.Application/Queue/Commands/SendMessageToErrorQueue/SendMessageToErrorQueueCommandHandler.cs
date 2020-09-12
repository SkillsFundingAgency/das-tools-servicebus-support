using SFA.DAS.Tools.Servicebus.Support.Infrastructure.Services;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application.Queue.Commands.SendMessageToErrorQueue
{
    public class SendMessageToErrorQueueCommandHandler : ICommandHandler<SendMessageToErrorQueueCommand, SendMessageToErrorQueueCommandResponse>
    {
        private readonly IAsbService _asbService;

        public SendMessageToErrorQueueCommandHandler(IAsbService asbService)
        {
            _asbService = asbService;
        }

        public async Task<SendMessageToErrorQueueCommandResponse> Handle(SendMessageToErrorQueueCommand query)
        {
            await _asbService.SendMessageToErrorQueueAsync(query.Message);

            return new SendMessageToErrorQueueCommandResponse();
        }
    }
}
